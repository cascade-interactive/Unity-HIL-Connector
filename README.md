![Unity](https://img.shields.io/badge/Unity-100000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Deprecated](https://img.shields.io/badge/Status-Deprecated-red?style=for-the-badge)

# Unity Hardware in the Loop Visualizer Connection

**Note:** This project contains an initial, basic version of a hardware-in-the-loop visualizer. Development on this Unity implementation is currently deprecated. The focus has shifted to an Unreal Engine 5 plugin version to act as a bidirectional bridge connecting the simulation state to an ESP32 flight computer.

## How It Works

This visualizer acts as a real-time rendering client for embedded physics simulations. It receives continuous state data over the network and translates it into smooth 3D motion.

### 1. UDP Data Reception
The `UDPReceiver` script handles the network layer by spinning up a dedicated background thread to listen for incoming UDP packets. As packets arrive from the flight computer or simulation server, they are placed into a thread-safe concurrent queue so the main Unity thread can process them without blocking.

### 2. Binary Packet Parsing
In the `VisualizationManager`, raw byte arrays are dequeued and cast into C# structs defined in `PacketDefinition.cs`. The system routes the data based on the payload type, unpacking physics state data (position, velocity, acceleration, quaternions, and gyro rates) or actuator commands.

### 3. Coordinate System Translation
External physics engines and aerospace frameworks typically use a Right-Handed Z-Up coordinate system. Unity uses a Left-Handed Y-Up system. The visualizer automatically maps the incoming coordinates by swapping the Y and Z positional axes and flipping the W component of the quaternion to ensure the orientation is represented accurately on screen.

### 4. Buffered Interpolation for Jitter Reduction
Directly applying network data to a Transform often causes visual stuttering due to network jitter. To solve this, the visualizer uses a timestamped state buffer. It calculates the exact blend percentage between the closest past and future packets in the buffer, using `Vector3.Lerp` for position and `Quaternion.Slerp` for rotation to guarantee smooth interpolation.
