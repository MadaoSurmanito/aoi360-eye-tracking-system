# Phase 0 Validation Checklist

Use this checklist when validating the current Unity runtime on device.

## 1. Scene and playback

- Open `Phase0_360Playback_VR`
- Enter Play mode
- Confirm the 360 video prepares successfully
- Confirm playback starts and the skybox updates correctly

## 2. AOI overlay

- Confirm `AOIOverlaySphere` appears at runtime
- Confirm AOIs are visible as a semi-transparent overlay on the 360 video
- Confirm the currently focused AOI becomes more visible than the rest

## 3. Eye tracking source

Check the debug overlay:

- `Tracking Source` should become `OpenXREyeGaze` or `ViveEyeTracker`
- It should not stay at `None` while eye tracking is active

If OpenXR gaze is not valid, the HTC fallback should still be able to drive the runtime when the VIVE eye tracker feature is enabled.

## 4. Fixation visualization

- Confirm the active fixation hit marker appears
- Confirm it grows when fixation remains stable
- Confirm a fixation trail is created
- Confirm the trail keeps at most `10` markers
- Confirm older trail markers are discarded first

## 5. AOI resolution

Check the debug overlay:

- `AOI ID` changes when gaze moves between AOIs
- `AOI Name` and `AOI Category` are populated when metadata exists
- `AOI Mode` matches the intended decoding mode

## 6. Pupil data

When HTC eye tracker data is available:

- `Pupils L/R` should show numeric values instead of `-`

If pupil values stay empty while gaze tracking works, verify that the HTC eye tracker extension is enabled and that the device supports pupil data in the current runtime.

## 7. CSV export

After a short run, export a CSV and verify:

- rows are fixation-based, not per-frame spam
- timestamps are aligned to the fixation cadence
- `aoi_id` and `aoi_confidence` are populated
- `left_pupil_diameter` and `right_pupil_diameter` are populated when available
- `is_valid` reflects whether tracking was valid

## 8. AOI texture import

If AOI lookup or overlay fails, verify the AOI texture import settings:

- `Read/Write Enabled`
- `Mip Maps Off`
- `Filter Mode Point`
- `Compression None`

Most apparent AOI-map bugs in Phase 0 come from importing a data texture as if it were a regular visual texture.
