# Phase 0 Documentation

Phase 0 is the first stable Unity prototype of the AOI360 system.

Its purpose is to validate the runtime experiment loop before the offline AOI generation pipeline is connected:

- play one 360 video in VR
- read real eye tracking data
- map gaze onto the equirectangular stimulus
- resolve AOI hits from a precomputed AOI map
- visualize fixations in the headset
- export fixation-based CSV data

## Scope

Phase 0 does not yet generate AOIs automatically. AOI maps are still manual or handcrafted test assets, but the runtime contract is already structured so the future Python pipeline can drop in generated maps and metadata without rewriting the Unity side.

## Key outputs

- runtime AOI hit detection
- AOI overlay visualization
- fixation commits every `250 ms`
- fixation trail history capped at `10` markers
- CSV export with AOI hit information
- pupil diameters when HTC eye tracker data is available

## Documents

- `runtime-unity.md` -> current Unity runtime behavior
- `aoi-data-contract.md` -> AOI map and metadata contract for Unity and Python
- `validation-checklist.md` -> practical checks for testing on device
