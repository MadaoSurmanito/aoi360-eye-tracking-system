# aoi360_pipeline

Offline Python pipeline for the AOI360 project.

## Current scope

The current branch covers the first usable offline loop:

1. extract sparse frames from a 360 video
2. run Grounding DINO over those frames
3. convert reviewed detections into a Unity-compatible AOI map and metadata JSON

This is not yet the final automated pipeline with segmentation, temporal propagation, seam handling, and tracking. It is the practical first version for testing AOI authoring against the Unity runtime.

## Install

Recommended:

```bash
pip install -e python/offline
```

Alternative:

```bash
pip install -r python/offline/requirements.txt
```

## Scripts

### 1. Extract frames

```bash
python python/offline/scripts/extract_frames.py --video-path data/input_videos/video_360.mp4 --output-dir data/frames/video_360 --every-n-frames 10
```

### 2. Run Grounding DINO

```bash
python python/offline/scripts/detect_grounding_dino.py --frames-dir data/frames/video_360 --output-csv data/interim/detections/video_360_grounding_dino_boxes.csv --text-prompt "person. face. bottle. screen. product."
```

### 3. Build an AOI map for Unity

```bash
python python/offline/scripts/build_aoi_map.py --detections-csv data/interim/detections/video_360_grounding_dino_boxes.csv --frames-dir data/frames/video_360 --output-map-path data/processed/id_maps/video_360_aoi_map.png --output-metadata-path data/processed/metadata/video_360_aoi_map_metadata.json --video-name video_360.mp4 --fps 30 --frame-index 0
```

## Outputs

- Extracted frames: `data/frames/<video_name>/`
- Detection CSV: `data/interim/detections/`
- AOI maps: `data/processed/id_maps/`
- AOI metadata: `data/processed/metadata/`

## Unity handoff

When a generated AOI map looks correct:

1. copy the PNG into `unity/AOI360Runtime/Assets/Textures/AOIMaps`
2. copy the metadata JSON into `unity/AOI360Runtime/Assets/StreamingAssets/AOIMaps`
3. set the AOI map import settings as a data texture:
   - Read/Write Enabled = On
   - Generate Mip Maps = Off
   - Filter Mode = Point
   - Compression = None
   - sRGB = Off when possible
