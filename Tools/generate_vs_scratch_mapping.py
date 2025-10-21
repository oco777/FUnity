#!/usr/bin/env python3
"""Scratch ↔ Visual Scripting 対応表を自動生成するスクリプトです。"""
from __future__ import annotations

import datetime
import pathlib
import re
from dataclasses import dataclass
from typing import Dict, List

BASE_DIR = pathlib.Path(__file__).resolve().parent.parent
UNITS_DIR = BASE_DIR / "Runtime" / "Integrations" / "VisualScripting" / "Units"
OUTPUT_PATH = BASE_DIR / "Docs" / "VS_Scratch_Mapping.txt"

UNIT_TITLE_PATTERN = re.compile(r"\[UnitTitle\(\"([^\"]+)\"\)\](?:.|\n)*?class\s+(\w+)\s*:\s*Unit", re.MULTILINE)

@dataclass
class NodeInfo:
    """対応表の1行を表すデータ構造です。"""

    title: str
    class_name: str
    category: str
    scratch_label: str
    note: str
    source_path: pathlib.Path | None = None


TRANSLATION_MAP: Dict[str, str] = {
    "Scratch/Move Steps": "◯歩動かす",
    "Scratch/Point Direction": "◯度に向ける",
    "Scratch/Turn Right": "◯度回す（右回り）",
    "Scratch/Turn Left": "◯度回す（左回り）",
    "Scratch/Turn Degrees": "◯度回す",
    "Scratch/Go To (x, y)": "x:◯ y:◯ へ行く",
    "Scratch/Go To X,Y": "x:◯ y:◯ へ行く",
    "Scratch/Set X": "x座標を ◯ にする",
    "Scratch/Change X by": "x座標を ◯ ずつ変える",
    "Scratch/Change X By": "x座標を ◯ ずつ変える",
    "Scratch/Set Y": "y座標を ◯ にする",
    "Scratch/Change Y by": "y座標を ◯ ずつ変える",
    "Scratch/Change Y By": "y座標を ◯ ずつ変える",
    "Scratch/Repeat (n)": "◯ 回繰り返す",
    "Scratch/Repeat N": "◯ 回繰り返す",
    "Scratch/Forever": "ずっと",
    "Scratch/Wait Seconds": "◯ 秒待つ",
    "Scratch/Say": "◯ と言う",
}

CATEGORY_MAP: Dict[str, str] = {
    "Scratch/Move Steps": "基本操作",
    "Scratch/Turn Degrees": "基本操作",
    "Scratch/Turn Right": "基本操作",
    "Scratch/Turn Left": "基本操作",
    "Scratch/Point Direction": "基本操作",
    "Scratch/Go To (x, y)": "基本操作",
    "Scratch/Go To X,Y": "基本操作",
    "Scratch/Set X": "基本操作",
    "Scratch/Change X by": "基本操作",
    "Scratch/Change X By": "基本操作",
    "Scratch/Set Y": "基本操作",
    "Scratch/Change Y by": "基本操作",
    "Scratch/Change Y By": "基本操作",
    "Scratch/Say": "表示・演出",
    "Scratch/Repeat (n)": "制御",
    "Scratch/Repeat N": "制御",
    "Scratch/Forever": "制御",
    "Scratch/Wait Seconds": "制御",
}

REQUIRED_TITLES: List[str] = [
    "Scratch/Move Steps",
    "Scratch/Turn Degrees",
    "Scratch/Point Direction",
    "Scratch/Go To X,Y",
    "Scratch/Set X",
    "Scratch/Change X By",
    "Scratch/Set Y",
    "Scratch/Change Y By",
    "Scratch/Repeat N",
    "Scratch/Forever",
    "Scratch/Wait Seconds",
    "Scratch/Say",
]


def collect_units() -> Dict[str, NodeInfo]:
    """UnitTitle 属性を持つクラスを走査し、辞書形式で返します。"""

    results: Dict[str, NodeInfo] = {}
    for cs_path in sorted(UNITS_DIR.rglob("*.cs")):
        name = cs_path.name
        if not (name.endswith("Unit.cs") or name.endswith("Units.cs")):
            continue
        text = cs_path.read_text(encoding="utf-8")
        for title, class_name in UNIT_TITLE_PATTERN.findall(text):
            if not (title.startswith("Scratch/") or title.startswith("Fooni/")):
                continue

            category = CATEGORY_MAP.get(title, "未分類")
            translation = TRANSLATION_MAP.get(title)
            note = ""
            if translation is None:
                translation = f"{title} ※TODO: 日本語訳を確認"
                note = "TODO: 日本語訳の精査が必要"

            results[title] = NodeInfo(
                title=title,
                class_name=class_name,
                category=category,
                scratch_label=translation,
                note=note,
                source_path=cs_path.relative_to(BASE_DIR),
            )

    return results


def ensure_required(nodes: Dict[str, NodeInfo]) -> List[NodeInfo]:
    """必須タイトルが存在するように一覧を拡張します。"""

    completed: List[NodeInfo] = []
    for title, info in nodes.items():
        completed.append(info)

    for title in REQUIRED_TITLES:
        if title in nodes:
            continue

        category = CATEGORY_MAP.get(title, "未分類")
        translation = TRANSLATION_MAP.get(title, f"{title} ※TODO: 日本語訳を確認")
        note = "未実装: 対応する Unit が見つかりません"
        completed.append(
            NodeInfo(
                title=title,
                class_name="(未実装)",
                category=category,
                scratch_label=translation,
                note=note,
            )
        )

    return completed


def render_document(nodes: List[NodeInfo]) -> str:
    """対応表ドキュメントを整形して返します。"""

    header = [
        "FUnity Visual Scripting 対応表（Scratch ブロック ↔ VS ノード）",
        "================================================================",
        "自動生成日時: " + datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "生成スクリプト: Tools/generate_vs_scratch_mapping.py",
        "",
        "各行形式: VSノード名 | Scratch日本語 | 実装クラス | 備考",
        "",
    ]

    nodes_sorted = sorted(nodes, key=lambda x: (x.category, x.title))

    sections: Dict[str, List[NodeInfo]] = {}
    for node in nodes_sorted:
        sections.setdefault(node.category, []).append(node)

    lines: List[str] = header
    for category, category_nodes in sections.items():
        lines.append(f"[{category}]")
        lines.append("VSノード名 | Scratch日本語 | 実装クラス | 備考")
        lines.append("-" * 72)
        for node in category_nodes:
            note = node.note
            if not note and node.source_path is not None:
                note = f"定義: {node.source_path}"
            lines.append(
                " | ".join([
                    node.title,
                    node.scratch_label,
                    node.class_name,
                    note,
                ])
            )
        lines.append("")

    return "\n".join(lines).rstrip() + "\n"


def main() -> None:
    """スクリプトのエントリポイントです。"""

    nodes = collect_units()
    completed_nodes = ensure_required(nodes)
    document = render_document(completed_nodes)
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(document, encoding="utf-8")
    print(f"Mapping file generated: {OUTPUT_PATH}")


if __name__ == "__main__":
    main()
