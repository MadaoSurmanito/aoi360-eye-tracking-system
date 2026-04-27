from __future__ import annotations

import argparse
import colorsys
import json
from pathlib import Path

import pandas as pd
from PIL import Image, ImageDraw


REQUIRED_DETECTION_COLUMNS = {
    "frame_index",
    "frame_file",
    "detection_index",
    "label",
    "confidence",
    "x_min",
    "y_min",
    "x_max",
    "y_max",
    "prompt",
}


def build_distinct_hex_color(index: int) -> str:
    if index < 1:
        raise ValueError("AOI color index must be >= 1")

    hue = (index * 0.61803398875) % 1.0
    red, green, blue = colorsys.hsv_to_rgb(hue, 0.75, 1.0)
    return "#{:02X}{:02X}{:02X}".format(int(red * 255), int(green * 255), int(blue * 255))


def parse_label_filters(values: list[str] | None) -> set[str]:
    if not values:
        return set()
    return {value.strip().lower() for value in values if value and value.strip()}


def build_aoi_map(
    detections_csv: str | Path,
    frames_dir: str | Path,
    output_map_path: str | Path,
    output_metadata_path: str | Path,
    video_name: str,
    fps: int = 30,
    frame_index: int | None = None,
    frame_file: str | None = None,
    include_labels: list[str] | None = None,
    min_confidence: float = 0.35,
    box_padding: int = 0,
) -> dict[str, object]:
    detections_csv = Path(detections_csv)
    frames_dir = Path(frames_dir)
    output_map_path = Path(output_map_path)
    output_metadata_path = Path(output_metadata_path)

    if not detections_csv.exists():
        raise FileNotFoundError(f"Detections CSV not found: {detections_csv}")

    if not frames_dir.exists():
        raise FileNotFoundError(f"Frames directory not found: {frames_dir}")

    detections = pd.read_csv(detections_csv)
    if detections.empty:
        raise RuntimeError(f"Detections CSV is empty: {detections_csv}")

    missing_columns = sorted(REQUIRED_DETECTION_COLUMNS.difference(detections.columns))
    if missing_columns:
        raise ValueError(
            "Detections CSV is missing required columns: " + ", ".join(missing_columns)
        )

    if frame_file and frame_index is not None:
        raise ValueError("Use either frame_file or frame_index, not both.")

    if box_padding < 0:
        raise ValueError("box_padding must be >= 0")

    if fps < 0:
        raise ValueError("fps must be >= 0")

    label_filters = parse_label_filters(include_labels)
    if label_filters:
        detections = detections[detections["label"].astype(str).str.lower().isin(label_filters)]

    detections = detections[detections["confidence"] >= float(min_confidence)].copy()
    if detections.empty:
        raise RuntimeError("No detections survived the current filters.")

    if frame_file:
        detections = detections[detections["frame_file"] == frame_file].copy()
    elif frame_index is not None:
        detections = detections[detections["frame_index"] == int(frame_index)].copy()
    else:
        chosen_frame_index = int(detections.sort_values(["frame_index", "detection_index"]).iloc[0]["frame_index"])
        detections = detections[detections["frame_index"] == chosen_frame_index].copy()

    if detections.empty:
        raise RuntimeError("No detections remain for the selected frame.")

    detections = detections.sort_values(["confidence", "detection_index"], ascending=[False, True]).reset_index(drop=True)

    selected_frame_file = str(detections.iloc[0]["frame_file"])
    selected_frame_index = int(detections.iloc[0]["frame_index"])
    selected_frame_path = frames_dir / selected_frame_file
    if not selected_frame_path.exists():
        raise FileNotFoundError(f"Frame image referenced by detections was not found: {selected_frame_path}")

    frame_image = Image.open(selected_frame_path).convert("RGB")
    width, height = frame_image.size

    aoi_map = Image.new("RGB", (width, height), (0, 0, 0))
    drawer = ImageDraw.Draw(aoi_map)

    aois: list[dict[str, object]] = []
    for aoi_id, row in enumerate(detections.itertuples(index=False), start=1):
        color_hex = build_distinct_hex_color(aoi_id)
        red = int(color_hex[1:3], 16)
        green = int(color_hex[3:5], 16)
        blue = int(color_hex[5:7], 16)

        x_min = max(0, int(round(float(row.x_min) - box_padding)))
        y_min = max(0, int(round(float(row.y_min) - box_padding)))
        x_max = min(width - 1, int(round(float(row.x_max) + box_padding)))
        y_max = min(height - 1, int(round(float(row.y_max) + box_padding)))

        # Phase 1 deliberately paints box AOIs as a bootstrap path. Once segmentation
        # lands, this fill step can be replaced by exact masks without changing the
        # Unity-side metadata contract.
        drawer.rectangle([x_min, y_min, x_max, y_max], fill=(red, green, blue))

        label = str(row.label)
        detection_index = int(row.detection_index)
        aois.append(
            {
                "id": aoi_id,
                "name": f"{label}_{aoi_id:02d}",
                "prompt": str(row.prompt),
                "category": label,
                "parentId": 0,
                "color": color_hex,
                "sourceDetectionIndex": detection_index,
                "confidence": float(row.confidence),
                "bbox": [x_min, y_min, x_max, y_max],
            }
        )

    output_map_path.parent.mkdir(parents=True, exist_ok=True)
    output_metadata_path.parent.mkdir(parents=True, exist_ok=True)
    aoi_map.save(output_map_path)

    metadata = {
        "video": video_name,
        "fps": int(fps),
        "idMapResolution": [width, height],
        "frameIndex": selected_frame_index,
        "frameFile": selected_frame_file,
        "aois": aois,
    }
    output_metadata_path.write_text(json.dumps(metadata, indent=2), encoding="utf-8")

    return {
        "frame_file": selected_frame_file,
        "frame_index": selected_frame_index,
        "aoi_count": len(aois),
        "output_map_path": str(output_map_path),
        "output_metadata_path": str(output_metadata_path),
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Build a Unity-compatible AOI map and metadata JSON from a detections CSV."
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
        "--output-map-path",
        default="data/processed/id_maps/video_360_aoi_map.png",
        help="Path where the AOI map PNG will be written.",
    )
    parser.add_argument(
        "--output-metadata-path",
        default="data/processed/metadata/video_360_aoi_map_metadata.json",
        help="Path where the AOI metadata JSON will be written.",
    )
    parser.add_argument(
        "--video-name",
        default="video_360.mp4",
        help="Video filename to write into the metadata JSON.",
    )
    parser.add_argument(
        "--fps",
        type=int,
        default=30,
        help="FPS metadata to write into the AOI metadata JSON.",
    )
    parser.add_argument(
        "--frame-index",
        type=int,
        default=None,
        help="Specific frame index to convert into an AOI map.",
    )
    parser.add_argument(
        "--frame-file",
        default=None,
        help="Specific frame filename to convert into an AOI map.",
    )
    parser.add_argument(
        "--include-label",
        action="append",
        dest="include_labels",
        help="Optional label filter. Repeat to keep multiple labels.",
    )
    parser.add_argument("--min-confidence", type=float, default=0.35)
    parser.add_argument("--box-padding", type=int, default=0)
    return parser


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()
    summary = build_aoi_map(
        detections_csv=args.detections_csv,
        frames_dir=args.frames_dir,
        output_map_path=args.output_map_path,
        output_metadata_path=args.output_metadata_path,
        video_name=args.video_name,
        fps=args.fps,
        frame_index=args.frame_index,
        frame_file=args.frame_file,
        include_labels=args.include_labels,
        min_confidence=args.min_confidence,
        box_padding=args.box_padding,
    )

    print(f"Frame used: {summary['frame_file']} (index={summary['frame_index']})")
    print(f"AOIs written: {summary['aoi_count']}")
    print(f"AOI map: {summary['output_map_path']}")
    print(f"Metadata: {summary['output_metadata_path']}")


if __name__ == "__main__":
    main()
