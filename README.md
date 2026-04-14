# AOI360 Eye Tracking System

System for 360º video eye tracking research with:
- offline AOI generation in Python
- experimental runtime in Unity
- post-hoc analytics in Python

## Architecture

### 1. Offline pipeline
Python pipeline for:
- frame extraction
- 360 projection handling
- AOI detection from prompts
- segmentation / tracking
- AOI ID map export
- metadata export

### 2. Runtime
Unity runtime for:
- 360 video playback
- HTC Vive Focus Vision eye tracking capture
- spherical gaze mapping
- AOI lookup per frame
- raw sample logging

### 3. Analytics
Python post-hoc analysis for:
- fixation detection
- TFF
- FD
- TFD
- FC
- FB
- validation comparisons between manual and automatic AOIs

## Phase 0 Goal
Build a minimal end-to-end prototype with:
- one 360 video
- 2-3 manual AOIs
- Unity skybox playback
- gaze capture
- AOI lookup
- CSV export
- basic metric calculation offline

## Repository layout

- `unity/AOI360Runtime` → Unity project
- `python/offline` → AOI generation pipeline
- `python/analytics` → metric analysis
- `data` → input/output data
- `docs` → architecture, ADRs, notes
- `experiments` → phase-specific experiments