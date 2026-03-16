using System.Runtime.InteropServices;

public static class PacketConstants
{
    public const uint PACKET_MAGIC = 0x4C594E4E;
    public const byte PHYSICS_PAYLOAD = 0x40;
    public const byte ACTUATOR_PAYLOAD = 0x25;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PacketHeader
{
    public uint magic;
    public byte version;
    public byte payload_type;
    public byte device_id;
    public byte flags;
    public uint sequence;
    public ushort length;
    public ushort reserved;
    public ulong timestamp_us;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhysicsStatePayload
{
    public double pos_x, pos_y, pos_z;
    public float vel_x, vel_y, vel_z;
    public float accel_x, accel_y, accel_z;
    public float quat_x, quat_y, quat_z, quat_w;
    public float gyro_x, gyro_y, gyro_z;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ActuatorPayload
{
    public byte actuator_id;
    public float command;
    public float feedback;
}
