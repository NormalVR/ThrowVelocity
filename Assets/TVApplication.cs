using UnityEngine;
using Valve.VR;

public class TVApplication : MonoBehaviour
{
    public  GameObject steamVRController;
    private SteamVR_TrackedObject _trackedObject;
    private GameObject _controller;
    private Rigidbody  _controllerRigidBody;
    private GameObject _cube;

    private Vector3 offset = new Vector3(0.0f, 0.1f, 0.2f);

    void Awake()
    {
        _trackedObject = steamVRController.GetComponent<SteamVR_TrackedObject>();

        // Create an empty game object. Attach a rigid body to use later for calculating velocity.
        _controller = new GameObject();
        _controllerRigidBody = _controller.AddComponent<Rigidbody>();
        _controllerRigidBody.isKinematic = true;

        CreateCube();
    }

    // Watch for new_poses event from SteamVR.
    void OnEnable()
    {
        SteamVR_Utils.Event.Listen("new_poses", OnNewPoses);
    }

    void OnDisable()
    {
        SteamVR_Utils.Event.Remove("new_poses", OnNewPoses);
    }

    // Parse and dispatch a PositionGeometry() call with the new controller pose.
    private void OnNewPoses(params object[] args)
    {
        if (_trackedObject.index == SteamVR_TrackedObject.EIndex.None)
            return;

        int i = (int)_trackedObject.index;

        TrackedDevicePose_t[] poses = (TrackedDevicePose_t[])args[0];
        if (poses.Length <= i)
            return;

        if (!poses[i].bDeviceIsConnected)
            return;

        if (!poses[i].bPoseIsValid)
            return;

        SteamVR_Utils.RigidTransform pose = new SteamVR_Utils.RigidTransform(poses[i].mDeviceToAbsoluteTracking);

        PositionGeometry(pose.pos, pose.rot);
    }

    void Update()
    {
        if (_trackedObject.index == SteamVR_TrackedObject.EIndex.None)
        {
            // No device
            // TODO: Cancel events that were in progress?
            return;
        }

        SteamVR_Controller.Device device = SteamVR_Controller.Input((int)_trackedObject.index);

        // Set our empty game object's rigid body to match the device's velocity and angular velocity.
        _controllerRigidBody.velocity        = device.velocity;
        _controllerRigidBody.angularVelocity = device.angularVelocity;

        // Dispatch trigger events
        bool triggerBegan  = device.GetHairTriggerDown();
        bool triggerActive = device.GetHairTrigger();
        bool triggerEnded  = device.GetHairTriggerUp();

        if (triggerBegan)  TriggerBegan();
        if (triggerActive) TriggerActive();
        if (triggerEnded)  TriggerEnded();
    }

    void TriggerBegan()
    {
        _cube.SetActive(true);
    }

    void TriggerActive()
    {

    }

    void TriggerEnded()
    {
        // Throw the cube by setting the velocity on its rigid body using the rigid body on the empty game object.
        Rigidbody rigidBody       = _cube.GetComponent<Rigidbody>();
        rigidBody.velocity        = _controllerRigidBody.GetPointVelocity(_controller.transform.TransformPoint(offset));
        rigidBody.angularVelocity = _controllerRigidBody.angularVelocity;
        rigidBody.isKinematic     = false;

        CreateCube();
    }

    void PositionGeometry(Vector3 position, Quaternion rotation)
    {
        // Position the empty game object.
        _controller.transform.position = position;
        _controller.transform.rotation = rotation;

        // Position cube to be thrown
        _cube.transform.position = position + rotation * offset;
        _cube.transform.rotation = rotation;
    }

    void CreateCube()
    {
        _cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        _cube.SetActive(false);

        Rigidbody rb   = _cube.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.mass        = 0.1f;
        
        //_cube.AddComponent<Sticky>();
    }
}
