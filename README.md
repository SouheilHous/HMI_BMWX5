# HMI_BMWX5

<img width="1392" alt="Screenshot 2024-12-11 at 17 43 59" src="https://github.com/user-attachments/assets/b722f92b-881a-455d-a929-ad38edd6d73e">

## Intro : 

[Demo Video](https://youtu.be/CYv63QAFTtg)

A Unity-based prototype for managing and simulating Human-Machine Interface (HMI) interactions for a BMW X5 vehicle. This project integrates features for controlling vehicle parts, dynamically adjusting rendering settings, and seamlessly switching between scenes with platform-dependent optimizations.

## Features

### 1. Vehicle Simulation
#### **Wheel Control**
- **Rotation**: Wheels rotate at a configurable speed.
- **Steering**: Front wheels steer in response to user inputs (left, right, center).
- **Back Wheel Adjustments**: Rear wheels reverse the angle appropriately for smoother steering.

#### **Door Control**
- **Front Doors**: Both doors open and close simultaneously with smooth animations.
- **Back Doors**: Both doors open and close simultaneously with reversed angles for left and right.
- **Back Hatch**: Opens and closes with smooth animations, adjustable angles.

<img width="1392" alt="Screenshot 2024-12-11 at 17 44 34" src="https://github.com/user-attachments/assets/b95c0110-fe6c-4405-8249-7229660eb105">

#### **Color Customization**
- Integrated color picker allows dynamic vehicle color changes.
- Updates the material's `_BaseColor` property in real time.
- Disables orbit camera rotation during color selection for a better user experience.

### 2. Rendering Management
- Dynamically adjusts render scale based on device type and memory:
  - **iOS**:
    - Memory ≥ 6 GB: Render scale = `1.5`
    - 4 GB ≤ Memory < 6 GB: Render scale = `1.0`
    - Memory < 4 GB: Render scale = `0.75`
  - **Android**:
    - Memory > 8 GB: Render scale = `1.5`
    - 5 GB ≤ Memory ≤ 8 GB: Render scale = `1.0`
    - Memory < 5 GB: Render scale = `0.75`

- Supports switching between different Universal Render Pipeline (URP) assets for each scene.

### 3. Scene Management
- **Scene Switching**:
  - Load Scene 1 or Scene 2 using UI buttons.
  - Configure URP assets or renderer indices for each scene dynamically.

- **Quality Settings**:
  - Applies specific URP assets or renderer settings per scene.
  - Ensures optimal performance based on platform and memory.

## Scripts Overview

### 1. `VehicleController`
Manages vehicle parts, including wheel rotation, steering, door animations, and color customization.

#### Key Features:
- **Methods**:
  - `ToggleWheelRotation()`: Starts/stops wheel rotation.
  - `SteerWheels(string direction)`: Steers the wheels left, right, or center.
  - `ToggleFrontDoors()`: Opens/closes front doors.
  - `ToggleBackDoors()`: Opens/closes back doors.
  - `ToggleBackDoor()`: Opens/closes the rear hatch.
- **Integration**:
  - Works seamlessly with the Unity GUI and color picker.

### 2. `SceneAndQualityManager`
Handles scene transitions and adjusts rendering settings dynamically.

#### Key Features:
- **Scene Management**:
  - `LoadScene1()`: Loads Scene 1 with corresponding URP or renderer settings.
  - `LoadScene2()`: Loads Scene 2 with corresponding URP or renderer settings.

- **Rendering Configuration**:
  - `SetURPAsset(UniversalRenderPipelineAsset)`: Applies a URP asset dynamically.
  - `SetPipelineRenderer(int rendererIndex)`: Adjusts the renderer index for the current URP asset.
  - `SetRenderScaleBasedOnDevice(UniversalRenderPipelineAsset)`: Sets render scale based on platform and memory.

### 3. `CUIColorPicker`
Custom UI color picker for real-time material color adjustments.

#### Key Features:
- **Real-Time Updates**:
  - Dynamically updates `_BaseColor` of the vehicle material.
  - Provides a smooth and user-friendly interface for color selection.
- **Input Handling**:
  - Disables orbit camera interactions during color selection.

## Setup and Usage

### Prerequisites
- Unity Editor (2021 or later recommended).
- Universal Render Pipeline (URP) setup.

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/SouheilHous/HMI_BMWX5.git
   ```
2. Open the project in Unity.
3. Assign necessary assets in the Inspector:
   - Vehicle materials and transforms.
   - URP assets for scenes.
4. Ensure scenes are added to the Build Settings.

### Running the Project
1. Play the scene in Unity Editor.
2. Use the GUI buttons for:
   - Switching scenes.
   - Controlling vehicle parts.
   - Changing vehicle color.
3. Deploy to iOS/Android for device-specific render scale adjustments.

### Performance Notes
- Scene 1 includes post-processing effects to simulate bloom and car head/tail lights.
- Scene 2 excludes post-processing for performance optimization.
- Tested on:
  - Xperia XZ1 (4 GB RAM): Smooth 60 FPS.
  - iPhone 13 Pro (6 GB RAM): Smooth 60 FPS.

## Customization
- **Add More Scenes**: Extend `SceneAndQualityManager` by adding new URP assets and renderer indices.
- **Modify GUI**: Adjust button styles and layouts in the `OnGUI` methods of respective scripts.
- **Enhance Color Picker**: Add more customization options for materials (e.g., metallic, smoothness).

## Known Issues
- Ensure URP assets are properly assigned to avoid null reference errors.
- Device memory detection relies on `SystemInfo.systemMemorySize`, which may vary slightly between devices.

## Contributing
Feel free to fork the repository and submit pull requests. Contributions are welcome to enhance functionality or optimize performance further.

## License
This project is licensed under the MIT License. See the LICENSE file for details.

## Acknowledgments
- Unity Technologies for the Universal Render Pipeline.
- Community contributors for inspiration and guidance.

---

### Author
**Souheil Elhoucine**

For any inquiries or support, feel free to reach out or open an issue on the GitHub repository.

[Project Repository](https://github.com/SouheilHous/HMI_BMWX5.git)

