#!/usr/bin/env python3
"""Exhaustive solver for Audere's endpoint-connected StepTile rules."""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


Point = tuple[int, int]


@dataclass(frozen=True)
class Piece:
    piece_id: str
    path: tuple[Point, ...]
    source_index: int


@dataclass(frozen=True)
class Move:
    piece_id: str
    source_index: int
    rotation: int
    path: tuple[Point, ...]

    @property
    def end(self) -> Point:
        return self.path[-1]


def rotate(point: Point, quarter_turns: int) -> Point:
    x, y = point
    return ((x, y), (-y, x), (-x, -y), (y, -x))[quarter_turns % 4]


def placements(piece: Piece, player: Point, cells: set[Point]) -> Iterable[Move]:
    seen: set[tuple[Point, ...]] = set()
    for rotation in range(4):
        local = tuple(rotate(point, rotation) for point in piece.path)
        for endpoint_index in (0, len(local) - 1):
            endpoint = local[endpoint_index]
            origin = (player[0] - endpoint[0], player[1] - endpoint[1])
            absolute = tuple((origin[0] + p[0], origin[1] + p[1]) for p in local)
            if endpoint_index == len(local) - 1:
                absolute = tuple(reversed(absolute))
            if absolute in seen:
                continue
            seen.add(absolute)
            if absolute[0] == player and all(point in cells for point in absolute[1:]):
                yield Move(piece.piece_id, piece.source_index, rotation * 90, absolute)


def validate_spec(spec: dict) -> tuple[int, int, Point, Point, set[Point], list[Piece], bool, int]:
    width = int(spec["width"])
    height = int(spec["height"])
    if width <= 0 or height <= 0:
        raise ValueError("width and height must be positive")

    start = tuple(map(int, spec["start"]))
    goal = tuple(map(int, spec["goal"]))
    cells = {tuple(map(int, value)) for value in spec["cells"]}
    if len(start) != 2 or len(goal) != 2:
        raise ValueError("start and goal must each contain two integers")
    if start not in cells or goal not in cells:
        raise ValueError("start and goal must both be present in cells")
    outside = [point for point in cells if not (0 <= point[0] < width and 0 <= point[1] < height)]
    if outside:
        raise ValueError(f"cells outside board bounds: {outside}")

    pieces: list[Piece] = []
    for index, raw in enumerate(spec["pieces"]):
        path = tuple(tuple(map(int, point)) for point in raw["path"])
        if len(path) < 2:
            raise ValueError(f"piece {raw.get('id', index)} needs at least two path points")
        if any(abs(a[0] - b[0]) + abs(a[1] - b[1]) != 1 for a, b in zip(path, path[1:])):
            raise ValueError(f"piece {raw.get('id', index)} path must be cardinally contiguous")
        pieces.append(Piece(str(raw.get("id", f"piece-{index}")), path, index))

    require_all = bool(spec.get("require_all", True))
    max_solutions = max(1, int(spec.get("max_solutions", 2000)))
    return width, height, start, goal, cells, pieces, require_all, max_solutions


def solve(spec: dict) -> dict:
    width, height, start, goal, cells, pieces, require_all, max_solutions = validate_spec(spec)
    solutions: list[tuple[Move, ...]] = []
    valid_first: set[Move] = set()
    winning_first: set[Move] = set()
    dead_end_states = 0
    premature_goal_hits = 0
    truncated = False

    def visit(player: Point, remaining: tuple[Piece, ...], route: tuple[Move, ...]) -> bool:
        nonlocal dead_end_states, premature_goal_hits, truncated
        if len(solutions) >= max_solutions:
            truncated = True
            return True
        if not remaining:
            if player == goal:
                solutions.append(route)
                if route:
                    winning_first.add(route[0])
                return True
            dead_end_states += 1
            return False

        progressed = False
        solved_below = False
        for slot, piece in enumerate(remaining):
            next_remaining = remaining[:slot] + remaining[slot + 1 :]
            for move in placements(piece, player, cells):
                progressed = True
                if not route:
                    valid_first.add(move)
                if move.end == goal and next_remaining and require_all:
                    premature_goal_hits += 1
                    continue
                if move.end == goal and (not require_all or not next_remaining):
                    solution = route + (move,)
                    solutions.append(solution)
                    winning_first.add(solution[0])
                    solved_below = True
                    if len(solutions) >= max_solutions:
                        truncated = True
                        return True
                    continue
                solved_below = visit(move.end, next_remaining, route + (move,)) or solved_below
        if not progressed:
            dead_end_states += 1
        return solved_below

    visit(start, tuple(pieces), tuple())

    total_steps = sum(len(piece.path) - 1 for piece in pieces)
    displacement_parity = (abs(goal[0] - start[0]) + abs(goal[1] - start[1])) % 2

    def move_json(move: Move) -> dict:
        return {
            "piece": move.piece_id,
            "source_index": move.source_index,
            "rotation": move.rotation,
            "path": [list(point) for point in move.path],
            "end": list(move.end),
        }

    trap_first = valid_first - winning_first
    return {
        "solved": bool(solutions),
        "solution_count": len(solutions),
        "solutions_truncated": truncated,
        "total_piece_steps": total_steps,
        "parity_matches": total_steps % 2 == displacement_parity,
        "valid_first_move_count": len(valid_first),
        "winning_first_move_count": len(winning_first),
        "trap_first_move_count": len(trap_first),
        "dead_end_state_count": dead_end_states,
        "premature_goal_hit_count": premature_goal_hits,
        "sample_trap_first_moves": [move_json(move) for move in sorted(trap_first, key=repr)[:10]],
        "sample_solutions": [[move_json(move) for move in solution] for solution in solutions[:10]],
        "board": render_board(width, height, cells, start, goal),
    }


def render_board(width: int, height: int, cells: set[Point], start: Point, goal: Point) -> str:
    rows: list[str] = []
    for y in range(height - 1, -1, -1):
        row: list[str] = []
        for x in range(width):
            point = (x, y)
            row.append("S" if point == start else "G" if point == goal else "#" if point in cells else ".")
        rows.append(f"{y:>2} " + " ".join(row))
    rows.append("   " + " ".join(str(x % 10) for x in range(width)))
    return "\n".join(rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, help="JSON spec path; stdin is used when omitted")
    parser.add_argument("--compact", action="store_true", help="emit compact JSON")
    args = parser.parse_args()
    try:
        raw = args.input.read_text(encoding="utf-8") if args.input else sys.stdin.read()
        report = solve(json.loads(raw))
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError) as error:
        print(json.dumps({"solved": False, "error": str(error)}, ensure_ascii=False, indent=2))
        return 2

    print(json.dumps(report, ensure_ascii=False, indent=None if args.compact else 2))
    return 0 if report["solved"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
