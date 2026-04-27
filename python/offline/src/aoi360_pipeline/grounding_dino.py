from __future__ import annotations

import argparse
import re
from pathlib import Path

import pandas as pd


DEFAULT_MODEL_ID = "IDEA-Research/grounding-dino-tiny"
DETECTION_COLUMNS = [
    "frame_index",
    "frame_file",
    "detection_index",
    "label",
    "confidence",
    "x_min",
    "y_min",
    "x_max",
    "y_max",
    "source",
    "model_id",
    "prompt",
]


def get_frame_index(frame_path: Path) -> int:
    match = re.search(r"frame_(\d+)", frame_path.stem)
    if not match:
        raise ValueError(f"Could not extract frame index from: {frame_path.name}")
    return int(match.group(1))


def _lazy_import_transformers_stack():
    try:
        import torch
        from PIL import Image
        from tqdm import tqdm
        from transformers import AutoModelForZeroShotObjectDetection, AutoProcessor
    except ImportError as exc:  # pragma: no cover - depends on local environment
        raise RuntimeError(
            "Grounding DINO dependencies are missing. Install the offline pipeline dependencies first."
        ) from exc

    return torch, Image, tqdm, AutoModelForZeroShotObjectDetection, AutoProcessor


def detect_frames(
    frames_dir: str | Path,
    output_csv: str | Path,
    text_prompt: str,
    box_threshold: float = 0.35,
    text_threshold: float = 0.25,
    model_id: str = DEFAULT_MODEL_ID,
) -> pd.DataFrame:
    torch, Image, tqdm, AutoModelForZeroShotObjectDetection, AutoProcessor = _lazy_import_transformers_stack()

    frames_dir = Path(frames_dir)
    output_csv = Path(output_csv)

    if not frames_dir.exists():
        raise FileNotFoundError(f"Frames directory not found: {frames_dir}")

    if not text_prompt or not text_prompt.strip():
        raise ValueError("text_prompt must not be empty")

    if not 0.0 <= box_threshold <= 1.0:
        raise ValueError("box_threshold must be between 0.0 and 1.0")

    if not 0.0 <= text_threshold <= 1.0:
        raise ValueError("text_threshold must be between 0.0 and 1.0")

    output_csv.parent.mkdir(parents=True, exist_ok=True)

    device = "cuda" if torch.cuda.is_available() else "cpu"
    print(f"Using device: {device}")
    print(f"Model: {model_id}")

    processor = AutoProcessor.from_pretrained(model_id)
    model = AutoModelForZeroShotObjectDetection.from_pretrained(model_id).to(device)
    model.eval()

    frame_paths = sorted([*frames_dir.glob("*.jpg"), *frames_dir.glob("*.jpeg"), *frames_dir.glob("*.png")])
    if not frame_paths:
        raise RuntimeError(f"No images found in: {frames_dir}")

    rows: list[dict[str, object]] = []

    for frame_path in tqdm(frame_paths, desc="Running Grounding DINO"):
        image = Image.open(frame_path).convert("RGB")
        frame_index = get_frame_index(frame_path)

        inputs = processor(images=image, text=text_prompt, return_tensors="pt").to(device)
        with torch.no_grad():
            outputs = model(**inputs)

        results = processor.post_process_grounded_object_detection(
            outputs,
            inputs.input_ids,
            box_threshold=box_threshold,
            text_threshold=text_threshold,
            target_sizes=[(image.height, image.width)],
        )[0]

        boxes = results["boxes"].cpu().tolist()
        scores = results["scores"].cpu().tolist()
        labels = [str(label) for label in results["labels"]]

        for detection_index, (box, score, label) in enumerate(zip(boxes, scores, labels)):
            x_min, y_min, x_max, y_max = box
            rows.append(
                {
                    "frame_index": frame_index,
                    "frame_file": frame_path.name,
                    "detection_index": detection_index,
                    "label": label,
                    "confidence": float(score),
                    "x_min": float(x_min),
                    "y_min": float(y_min),
                    "x_max": float(x_max),
                    "y_max": float(y_max),
                    "source": "grounding_dino",
                    "model_id": model_id,
                    "prompt": text_prompt,
                }
            )

    detections = pd.DataFrame(rows, columns=DETECTION_COLUMNS)
    detections.to_csv(output_csv, index=False)

    print(f"Frames processed: {len(frame_paths)}")
    print(f"Detections exported: {len(detections)}")
    print(f"CSV written to: {output_csv}")
    return detections


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Run Grounding DINO over extracted 360 frames and export bounding boxes to CSV."
    )
    parser.add_argument(
        "--frames-dir",
        default="data/frames/video_360",
        help="Directory containing the extracted frames.",
    )
    parser.add_argument(
        "--output-csv",
        default="data/interim/detections/video_360_grounding_dino_boxes.csv",
        help="CSV path where detections will be written.",
    )
    parser.add_argument(
        "--text-prompt",
        default="person. face. bottle. screen. product.",
        help="Grounding DINO text prompt. Dot-separated prompts work well for the model.",
    )
    parser.add_argument("--box-threshold", type=float, default=0.35)
    parser.add_argument("--text-threshold", type=float, default=0.25)
    parser.add_argument("--model-id", default=DEFAULT_MODEL_ID)
    return parser


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()
    detect_frames(
        frames_dir=args.frames_dir,
        output_csv=args.output_csv,
        text_prompt=args.text_prompt,
        box_threshold=args.box_threshold,
        text_threshold=args.text_threshold,
        model_id=args.model_id,
    )


if __name__ == "__main__":
    main()
