# Unity Runtime - Phase 0

## Active scene

The production scene for Phase 0 is:

- `Assets/Scenes/Phase0_360Playback_VR.unity`

Other scenes in the Unity project are considered older experiments or samples.

## Runtime modules

### Video playback

`VideoPlayback` prepares and plays a 360 video from `StreamingAssets/Videos/` onto the skybox render texture.

Responsibilities:
- prepare the video before the experiment starts
- expose current frame and time
- keep playback deterministic for logging

### Eye tracking

`EyeGazeSystem` is the runtime entry point for gaze data.

Current behavior:
- reads `<EyeGaze>/pose/position`
- reads `<EyeGaze>/pose/rotation`
- reads `<EyeGaze>/pose/isTracked`
- falls back to HTC VIVE eye tracker API when the standard OpenXR path is not valid
- exposes the active tracking source
- exposes pupil diameters when HTC data is available

Tracking sources:
- `OpenXREyeGaze`
- `ViveEyeTracker`
- `None`

### Spherical mapping

`GazeProviderBridge` forwards the world-space gaze direction into `SphericalMapper`.

`SphericalMapper` converts gaze direction into:
- azimuth
- elevation
- UV coordinates on the equirectangular map

### AOI lookup

`AOILookup` resolves AOI hits from the AOI texture using the current UV.

Supported modes:
- `MetadataExactColor`
- `Grayscale8Bit`
- `LegacyDominantRgb`

For the future pipeline, `MetadataExactColor` is the preferred mode.

### AOI overlay

`Phase0Bootstrap` creates a runtime sphere inside the 360 environment and renders a semi-transparent AOI overlay on top of the video.

Behavior:
- invisible background for non-AOI pixels
- regular opacity for AOIs
- boosted opacity for the currently focused AOI

### Fixation visualization

`EyeGazeDebugVisualizer` includes a lightweight fixation detector for runtime debugging.

Current behavior:
- fixation commit interval: `250 ms`
- angular stability threshold: approximately `3 degrees`
- visible hit marker for the active fixation
- persistent fixation trail
- trail capped to `10` markers
- nearby repeated fixations merge into the latest trail marker instead of creating duplicates

### Logging

`DataRecorder` exports fixation-based CSV rows instead of raw per-frame samples.

Fields currently exported:
- participant and session identifiers
- video identifier
- fixation timestamp in milliseconds
- current video frame
- gaze origin
- gaze direction
- spherical angles
- UV coordinates
- AOI id
- AOI confidence
- left and right pupil diameter when available
- validity flag

## Why fixation-based logging

Phase 0 uses fixation commits instead of full-rate raw streaming because the immediate goal is to validate:
- AOI alignment
- fixation timing
- experimental flow
- downstream analytics contracts

Raw high-frequency sample logging can still be added later if the study protocol needs it.
