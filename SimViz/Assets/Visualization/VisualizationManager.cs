using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(UDPReceiver))]
public class VisualizationManager : MonoBehaviour
{
    [Header("Target Objects")]
    [Tooltip("The GameObject to move and rotate based on physics payloads")]
    public Transform targetTransform;

    [Header("Interpolation Settings")]
    [Tooltip("How many seconds in the past to render the visualizer. 0.2s is good for smoothing network jitter.")]
    [Range(0f, 1f)]
    public float renderDelay = 0.2f;

    private UDPReceiver udpReceiver;

    // A struct to hold a snapshot of the physics state and WHEN it arrived
    private struct BufferedState
    {
        public float timeReceived;
        public Vector3 position;
        public Quaternion rotation;
    }

    // Our history of received states
    private List<BufferedState> stateBuffer = new List<BufferedState>();

    void Start()
    {
        udpReceiver = GetComponent<UDPReceiver>();
    }

    void Update()
    {
        // 1. Drain the network queue and add new states to our buffer
        while (udpReceiver.PacketQueue.TryDequeue(out byte[] data))
        {
            ProcessPacket(data);
        }

        // 2. Interpolate the target transform
        if (targetTransform != null)
        {
            UpdateInterpolation();
        }
    }

    private void ProcessPacket(byte[] data)
    {
        int headerSize = Marshal.SizeOf(typeof(PacketHeader));
        if (data.Length < headerSize) return;

        PacketHeader header = ByteArrayToStructure<PacketHeader>(data, 0);

        if (header.magic != PacketConstants.PACKET_MAGIC) return;

        if (header.payload_type == PacketConstants.PHYSICS_PAYLOAD)
        {
            if (data.Length >= headerSize + Marshal.SizeOf(typeof(PhysicsStatePayload)))
            {
                var physics = ByteArrayToStructure<PhysicsStatePayload>(data, headerSize);
                StorePhysicsState(physics);
            }
        }
        else if (header.payload_type == PacketConstants.ACTUATOR_PAYLOAD)
        {
            if (data.Length >= headerSize + Marshal.SizeOf(typeof(ActuatorPayload)))
            {
                var actuator = ByteArrayToStructure<ActuatorPayload>(data, headerSize);
                // Handle actuator (events usually don't need interpolation, execute immediately)
                Debug.Log($"[Visualizer] Actuator {actuator.actuator_id} commanded to {actuator.command}");
            }
        }
    }

    private void StorePhysicsState(PhysicsStatePayload physics)
    {
        // Apply Axis Swapping here (Assuming Sim is Right-Handed Z-Up)
        // Simulator (X, Y, Z) -> Unity (X, Z, Y)
        Vector3 mappedPos = new Vector3(
            (float)physics.pos_x,
            (float)physics.pos_z, // Z becomes Y
            (float)physics.pos_y  // Y becomes Z
        );

        // Corresponding quaternion swap (Right-Handed Z-Up -> Left-Handed Y-Up)
        Quaternion mappedRot = new Quaternion(
            physics.quat_x,
            physics.quat_z,
            physics.quat_y,
            -physics.quat_w // Flip W
        );

        // Save it to the buffer with the exact time Unity received it
        stateBuffer.Add(new BufferedState
        {
            timeReceived = Time.time,
            position = mappedPos,
            rotation = mappedRot
        });
    }

    private void UpdateInterpolation()
    {
        if (stateBuffer.Count == 0) return;

        // The exact time we want to draw on the screen
        float renderTime = Time.time - renderDelay;

        // Find the first state in the buffer that is NEWER than our renderTime
        int indexB = -1;
        for (int i = 0; i < stateBuffer.Count; i++)
        {
            if (stateBuffer[i].timeReceived > renderTime)
            {
                indexB = i;
                break;
            }
        }

        if (indexB == -1)
        {
            // All states are older than our render time (we are starving for packets)
            // Snap to the very last received state
            BufferedState latest = stateBuffer[stateBuffer.Count - 1];
            targetTransform.position = latest.position;
            targetTransform.rotation = latest.rotation;
        }
        else if (indexB == 0)
        {
            // All states are newer than our render time (we just started and don't have enough history)
            // Snap to the oldest state we have
            BufferedState oldest = stateBuffer[0];
            targetTransform.position = oldest.position;
            targetTransform.rotation = oldest.rotation;
        }
        else
        {
            // Perfect scenario: We found a state just BEFORE renderTime, and one just AFTER renderTime
            BufferedState stateA = stateBuffer[indexB - 1];
            BufferedState stateB = stateBuffer[indexB];

            // Calculate the blending percentage (t) between State A and State B
            float timeDiff = stateB.timeReceived - stateA.timeReceived;
            float t = (renderTime - stateA.timeReceived) / timeDiff;

            // Interpolate Position smoothly
            targetTransform.position = Vector3.Lerp(stateA.position, stateB.position, t);

            // Interpolate Rotation smoothly
            targetTransform.rotation = Quaternion.Slerp(stateA.rotation, stateB.rotation, t);

            // Housekeeping: Erase old states from the list so memory doesn't grow forever
            // We keep stateA around for the next frame's math just in case, but delete everything before it
            if (indexB - 1 > 0)
            {
                stateBuffer.RemoveRange(0, indexB - 1);
            }
        }
    }

    private T ByteArrayToStructure<T>(byte[] bytes, int offset) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(bytes, offset, ptr, size);
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}