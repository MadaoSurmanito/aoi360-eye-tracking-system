from __future__ import annotations

import argparse
import json
from pathlib import Path

from aoi360_pipeline.aoi_map_builder import (
    load_and_filter_detections,
    render_aoi_map_from_detections,
)


def build_aoi_sequence(
    detections_csv: str | Path,
    frames_dir: str | Path,
    output_maps_dir: str | Path,
    output_metadata_dir: str | Path,
    video_name: str,
    fps: int = 30,
    include_labels: list[str] | None = None,
    min_confidence: float = 0.35,
    box_padding: int = 0,
    max_frames: int | None = None,
    manifest_path: str | Path | None = None,
    skip_existing: bool = False,
) -> dict[str, object]:
    detections = load_and_filter_detections(
        detections_csv=detections_csv,
        include_labels=include_labels,
        min_confidence=min_confidence,
    )

    output_maps_dir = Path(output_maps_dir)
    output_metadata_dir = Path(output_metadata_dir)
    output_maps_dir.mkdir(parents=True, exist_ok=True)
    output_metadata_dir.mkdir(parents=True, exist_ok=True)

    frame_groups = list(detections.groupby(["frame_index", "frame_file"], sort=True))
    if max_frames is not None:
        frame_groups = frame_groups[: max(0, max_frames)]

    manifest_entries: list[dict[str, object]] = []
    written_count = 0

    for (frame_index, frame_file), frame_detections in frame_groups:
        frame_stem = Path(str(frame_file)).stem
        output_map_path = output_maps_dir / f"{frame_stem}_aoi_map.png"
        output_metadata_path = output_metadata_dir / f"{frame_stem}_aoi_map_metadata.json"

        if skip_existing and output_map_path.exists() and output_metadata_path.exists():
            manifest_entries.append(
                {
                    "frameIndex": int(frame_index),
                    "frameFile": str(frame_file),
                    "mapFile": output_map_path.name,
                    "metadataFile": output_metadata_path.name,
                    "aoiCount": None,
                    "skipped": True,
                }
            )
            continue

        summary = render_aoi_map_from_detections(
            detections=frame_detections.sort_values(["confidence", "detection_index"], ascending=[False, True]).reset_index(drop=True),
            frames_dir=frames_dir,
            output_map_path=output_map_path,
            output_metadata_path=output_metadata_path,
            video_name=video_name,
            fps=fps,
            box_padding=box_padding,
        )

        manifest_entries.append(
            {
                "frameIndex": int(summary["frame_index"]),
                "frameFile": summary["frame_file"],
                "mapFile": output_map_path.name,
                "metadataFile": output_metadata_path.name,
                "aoiCount": int(summary["aoi_count"]),
                "skipped": False,
            }
        )
        written_count += 1

    manifest = {
        "video": video_name,
        "fps": int(fps),
        "mapsDirectory": str(output_maps_dir),
        "metadataDirectory": str(output_metadata_dir),
        "frameCount": len(manifest_entries),
        "writtenCount": written_count,
        "frames": manifest_entries,
    }

    if manifest_path is not None:
        manifest_path = Path(manifest_path)
        manifest_path.parent.mkdir(parents=True, exist_ok=True)
        manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    return manifest


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Build Unity-compatible AOI maps and metadata JSON files for every detected frame."
    )
    parser.add_argument(
        "--detections-csv",
        default="data/interim/detections/video_360_grounding_dino_boxes.csv",
        help="Detections CSV produced by the Grounding DINO step.",
    )
    parser.add_argument(
        "--frames-dir",
        default="data/frames/video_360",
        help="Directory that contains the extracted frame images.",
    )
    parser.add_argument(
        "--output-maps-dir",
        default="data/processed/id_maps/video_360",
        help="Directory where one AOI map PNG per frame will be written.",
    )
    parser.add_argument(
        "--output-metadata-dir",
        default="data/processed/metadata/video_360",
        help="Directory where one AOI metadata JSON per frame will be written.",
    )
    parser.add_argument(
        "--manifest-path",
        default="data/processed/metadata/video_360_aoi_sequence_manifest.json",
        help="JSON manifest summarizing all per-frame AOI exports.",
    )
    parser.add_argument(
        "--video-name",
        default="video_360.mp4",
        help="Video filename to write into each metadata JSON and the sequence manifest.",
    )
    parser.add_argument(
        "--fps",
        type=int,
        default=30,
        help="FPS metadata to write into the AOI metadata JSON files.",
    )
    parser.add_argument(
        "--include-label",
        action="append",
        dest="include_labels",
        help="Optional label filter. Repeat to keep multiple labels.",
    )
    parser.add_argument("--min-confidence", type=float, default=0.35)
    parser.add_argument("--box-padding", type=int, default=0)
    parser.add_argument("--max-frames", type=int, default=None)
    parser.add_argument("--skip-existing", action="store_true")
    return parser


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()
    manifest = build_aoi_sequence(
        detections_csv=args.detections_csv,
        frames_dir=args.frames_dir,
        output_maps_dir=args.output_maps_dir,
        output_metadata_dir=args.output_metadata_dir,
        video_name=args.video_name,
        fps=args.fps,
        include_labels=args.include_labels,
        min_confidence=args.min_confidence,
        box_padding=args.box_padding,
        max_frames=args.max_frames,
        manifest_path=args.manifest_path,
        skip_existing=args.skip_existing,
    )

    print(f"Frames in manifest: {manifest['frameCount']}")
    print(f"Frames written now: {manifest['writtenCount']}")
    print(f"Maps directory: {manifest['mapsDirectory']}")
    print(f"Metadata directory: {manifest['metadataDirectory']}")


if __name__ == "__main__":
    main()
