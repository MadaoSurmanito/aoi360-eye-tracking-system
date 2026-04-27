"""
Utilities for the offline AOI360 pipeline.

The package intentionally keeps the first phase simple:
- extract sparse frames from a 360 video
- run Grounding DINO over those frames
- convert accepted detections into a Unity-compatible AOI map + metadata JSON
"""

__all__ = [
    "aoi_map_builder",
    "frame_extraction",
    "grounding_dino",
]
