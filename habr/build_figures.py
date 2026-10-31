import csv
import re
from datetime import date
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.lines import Line2D
from matplotlib.patches import Patch, PathPatch
from matplotlib.path import Path as ShapePath

ROOT = Path(__file__).resolve().parent
REPORT = ROOT.parent / "docs" / "report.md"
OUT = ROOT / "figures"
STEPS_CSV = ROOT / "data" / "release-steps.csv"

SURFACE = "#ffffff"
INK = "#0b0b0b"
INK_2 = "#52514e"
GRID = "#e1e0d9"
AXIS = "#c3c2b7"
FASTER = "#2a78d6"
SLOWER = "#e34948"
UNCHANGED = "#b5b3ab"
NOISE_BAND = "#f0efec"
STAND = "#52514e"
STOCK = "#6250d6"
COVER_SURFACE = "#14161a"
COVER_FASTER = "#5aa2f0"
COVER_SLOWER = "#f0625f"
COVER_UNCHANGED = "#5c5f66"
COVER_TEXT = "#f0efec"
COVER_TEXT_2 = "#c3c2b7"

POINTS = ["e8", "e9", "e10", "e11"]
VERSIONS = ["7.0", "8.0", "9.0", "10.0", "11 RC"]
WIDTH = 7.0
TITLE_SIZE = 14
SUBTITLE_SIZE = 10.5
LABEL_SIZE = 10.5
SMALL_SIZE = 9.5
LINE_INCHES = 0.2

plt.rcParams.update(
    {
        "font.family": "Liberation Sans",
        "font.size": LABEL_SIZE,
        "axes.edgecolor": AXIS,
        "axes.labelcolor": INK_2,
        "axes.facecolor": SURFACE,
        "figure.facecolor": SURFACE,
        "savefig.facecolor": SURFACE,
        "xtick.color": INK_2,
        "ytick.color": INK_2,
        "axes.spines.top": False,
        "axes.spines.right": False,
        "legend.frameon": False,
    }
)


def percent(value, signed=True, digits=0):
    text = f"{abs(value):.{digits}f}".replace(".", ",")
    if not signed:
        return f"{text}%"
    sign = "−" if value < 0 else "+"
    return f"{sign}{text}%"


def section(lines, start, stop):
    first = lines.index(start)
    last = next(i for i in range(first + 1, len(lines)) if lines[i].startswith(stop))
    return lines[first:last]


def parse_changes(block):
    rows = {}
    for line in block:
        cells = [cell.strip() for cell in line.strip("|").split("|")]
        if len(cells) < 8 or not cells[1].startswith("`"):
            continue
        values = {}
        for key, cell in zip(POINTS, cells[4:8]):
            match = re.match(r"([+-][\d.]+)%", cell)
            values[key] = float(match[1]) if match else None
        rows[cells[1].strip("`")] = values
    return rows


def parse_allocations(lines):
    block = section(lines, "### 4.3 Allocated bytes per batch", "## 5. Two resource")
    rows = {}
    for line in block:
        cells = [cell.strip() for cell in line.strip("|").split("|")]
        if len(cells) < 7 or not cells[0].startswith("`"):
            continue
        baseline = cells[2].split()[0].replace(",", "")
        current = cells[5].split()[0].replace(",", "")
        if baseline.isdigit() and current.isdigit() and int(baseline) > 0:
            rows[cells[0].strip("`")] = (int(current) / int(baseline) - 1) * 100
    return rows


def load():
    lines = REPORT.read_text(encoding="utf8").split("\n")
    fixed = parse_changes(
        section(lines, "#### Full resources (performance, turbo off)", "#### Limited")
    )
    stock = parse_changes(
        section(lines, "#### Limited resources (powersave, turbo on)", "### 5.4")
    )
    return fixed, stock, parse_allocations(lines)


def save(figure, name):
    OUT.mkdir(exist_ok=True)
    figure.savefig(OUT / name, dpi=220, facecolor=figure.get_facecolor())
    plt.close(figure)


def header(figure, height, title, subtitle, colours=(INK, INK_2), width=WIDTH):
    left = 0.1 / width
    figure.text(left, 1 - 0.1 / height, title, ha="left", va="top", fontsize=TITLE_SIZE, weight="bold", color=colours[0])
    lines = subtitle.count("\n") + 1
    figure.text(left, 1 - 0.38 / height, subtitle, ha="left", va="top", fontsize=SUBTITLE_SIZE, color=colours[1], linespacing=1.3)
    return 0.42 + lines * LINE_INCHES + 0.12


def canvas(height, left, right, bottom, title, subtitle, reserve=0.0):
    figure = plt.figure(figsize=(WIDTH, height))
    top = header(figure, height, title, subtitle) + reserve
    axis = figure.add_axes([left / WIDTH, bottom / height, (WIDTH - left - right) / WIDTH, (height - top - bottom) / height])
    return figure, axis


def axis_inches(axis):
    box = axis.get_window_extent().transformed(axis.figure.dpi_scale_trans.inverted())
    return box.width, box.height


def signed_percent_axis(axis, which):
    target = axis.xaxis if which == "x" else axis.yaxis
    target.set_major_formatter(lambda value, _: "0" if value == 0 else percent(value))


def recessive_grid(axis, which):
    axis.grid(axis=which, color=GRID, linewidth=0.7)
    axis.set_axisbelow(True)
    axis.tick_params(length=0)


def rounded_bar(axis, row, value, thickness, colour):
    width_inches, height_inches = axis_inches(axis)
    x_low, x_high = axis.get_xlim()
    y_low, y_high = axis.get_ylim()
    x_per_inch = (x_high - x_low) / width_inches
    y_per_inch = abs(y_high - y_low) / height_inches
    sign = 1 if value >= 0 else -1
    x_radius = min(0.04 * x_per_inch, abs(value))
    y_radius = min(0.04 * y_per_inch, thickness / 2)
    top = row - thickness / 2
    bottom = row + thickness / 2
    shoulder = value - sign * x_radius
    vertices = [
        (0, top),
        (shoulder, top),
        (value, top),
        (value, top + y_radius),
        (value, bottom - y_radius),
        (value, bottom),
        (shoulder, bottom),
        (0, bottom),
        (0, top),
    ]
    codes = [
        ShapePath.MOVETO,
        ShapePath.LINETO,
        ShapePath.CURVE3,
        ShapePath.CURVE3,
        ShapePath.LINETO,
        ShapePath.CURVE3,
        ShapePath.CURVE3,
        ShapePath.LINETO,
        ShapePath.CLOSEPOLY,
    ]
    axis.add_patch(PathPatch(ShapePath(vertices, codes), facecolor=colour, edgecolor="none", zorder=2))


def spread(values, gap):
    order = sorted(range(len(values)), key=lambda index: values[index])
    placed = [values[index] for index in order]
    for _ in range(50):
        for lower in range(len(placed) - 1):
            missing = gap - (placed[lower + 1] - placed[lower])
            if missing > 0:
                placed[lower] -= missing / 2
                placed[lower + 1] += missing / 2
    result = [0.0] * len(values)
    for position, index in enumerate(order):
        result[index] = placed[position]
    return result


def swarm(values, inches_per_unit, diameter):
    placed = []
    offsets = {}
    for index in sorted(range(len(values)), key=lambda item: abs(values[item])):
        horizontal = values[index] * inches_per_unit
        level = 0
        while True:
            candidates = [0.0] if level == 0 else [level * diameter * 0.5, -level * diameter * 0.5]
            free = [
                candidate
                for candidate in candidates
                if all((horizontal - x) ** 2 + (candidate - y) ** 2 >= diameter**2 for x, y in placed)
            ]
            if free:
                offsets[index] = free[0]
                placed.append((horizontal, free[0]))
                break
            level += 1
    return [offsets[index] for index in range(len(values))]


def direction_colour(value):
    return FASTER if value <= -5 else SLOWER if value >= 5 else UNCHANGED


def cover(fixed):
    height = 4.4
    width = 7.8
    figure = plt.figure(figsize=(width, height), facecolor=COVER_SURFACE)
    top = header(
        figure,
        height,
        "Время 46 операций .NET относительно версии 7.0",
        "Синие ускорились, красные замедлились, серые в пределах ±5%",
        (COVER_TEXT, COVER_TEXT_2),
        width,
    )
    axis = figure.add_axes([0.35 / width, 0.42 / height, (width - 0.35 - 2.2) / width, (height - top - 0.42) / height])
    axis.set_facecolor(COVER_SURFACE)
    highlights = {
        "options.snapshot-new-scope.x1k": "IOptionsSnapshot",
        "config.bind-poco50.x1k": "биндинг POCO",
        "logging.logger-message-define.x10k": "LoggerMessage",
    }
    positions = range(5)
    ordered = sorted(fixed.items(), key=lambda item: item[0] in highlights)
    for scope, values in ordered:
        series = [0.0] + [values[key] for key in POINTS]
        known = [(x, y) for x, y in zip(positions, series) if y is not None]
        xs, ys = zip(*known)
        tail = ys[-1]
        if scope in highlights:
            colour = COVER_FASTER if tail < -5 else COVER_SLOWER
            axis.plot(xs, ys, color=colour, linewidth=2.2, solid_capstyle="round", zorder=4)
            axis.plot(xs[-1], ys[-1], "o", color=colour, markersize=7, markeredgecolor=COVER_SURFACE, markeredgewidth=1.5, zorder=5)
            axis.text(xs[-1] + 0.1, ys[-1], f"{highlights[scope]} {percent(tail)}", va="center", ha="left", color=COVER_TEXT, fontsize=11.5, weight="bold", clip_on=False)
            continue
        colour = COVER_FASTER if tail < -5 else COVER_SLOWER if tail > 5 else COVER_UNCHANGED
        axis.plot(xs, ys, color=colour, linewidth=1, alpha=0.5, solid_capstyle="round", zorder=2)
    axis.axhline(0, color=COVER_TEXT_2, linewidth=0.8, zorder=1)
    axis.text(4.1, 0, "без изменений", va="center", ha="left", color=COVER_TEXT_2, fontsize=10, clip_on=False)
    for x, label in ((0.06, "ChatGPT"), (2.52, "первый PR агента")):
        axis.plot([x, x], [-48, 168], color=COVER_TEXT_2, linewidth=0.8, zorder=1)
        axis.text(x + 0.04, 168, label, va="top", ha="left", color=COVER_TEXT_2, fontsize=10)
    axis.set_ylim(-50, 170)
    axis.set_xlim(-0.08, 4.05)
    axis.set_xticks(list(positions), VERSIONS)
    axis.set_yticks([])
    axis.tick_params(length=0, colors=COVER_TEXT, labelsize=11.5)
    for spine in axis.spines.values():
        spine.set_visible(False)
    save(figure, "cover.png")


def epochs():
    height = 2.85
    figure = plt.figure(figsize=(WIDTH, height))
    top = header(figure, height, "Шесть точек замера и три эпохи", "Граница эпох условна: автодополнение Copilot появилось ещё в 2021 году")
    axis = figure.add_axes([0.1 / WIDTH, 0.05 / height, (WIDTH - 0.2) / WIDTH, (height - top - 0.05) / height])
    chatgpt = date(2022, 11, 30)
    agent = date(2025, 5, 19)
    start, end = date(2021, 6, 1), date(2027, 2, 1)
    bands = [
        (start, chatgpt, "#f0efec", "до ChatGPT"),
        (chatgpt, agent, "#cde2fb", "ИИ-ассистенты"),
        (agent, end, "#86b6ef", "агент открывает PR"),
    ]
    for left, right, colour, label in bands:
        axis.fill_between([left, right], 0.84, 1.0, color=colour, linewidth=0)
        axis.text(left + (right - left) / 2, 0.92, label, ha="center", va="center", color=INK, fontsize=LABEL_SIZE, weight="bold")
    axis.plot([start, end], [0.4, 0.4], color=AXIS, linewidth=1.2, zorder=1)
    releases = [
        (date(2021, 11, 8), "6.0", "нояб. 2021"),
        (date(2022, 11, 8), "7.0, база", "нояб. 2022"),
        (date(2023, 11, 14), "8.0", "нояб. 2023"),
        (date(2024, 11, 12), "9.0", "нояб. 2024"),
        (date(2025, 11, 11), "10.0", "нояб. 2025"),
        (date(2026, 10, 25), "11 RC3", "окт. 2026"),
    ]
    for moment, name, when in releases:
        axis.plot([moment], [0.4], "o", color=INK, markersize=8, markeredgecolor=SURFACE, markeredgewidth=2, zorder=3)
        axis.text(moment, 0.6, name, ha="center", va="bottom", color=INK, fontsize=LABEL_SIZE, weight="bold")
        axis.text(moment, 0.49, when, ha="center", va="bottom", color=INK_2, fontsize=SMALL_SIZE)
    for moment, label in ((chatgpt, "ChatGPT\n30 нояб. 2022"), (agent, "первый PR агента в dotnet/runtime\n19 мая 2025")):
        axis.plot([moment, moment], [0.4, 0.26], color=INK_2, linewidth=0.9, zorder=2)
        axis.text(moment, 0.23, label, ha="center", va="top", color=INK_2, fontsize=SMALL_SIZE, linespacing=1.3)
    axis.set_xlim(start, end)
    axis.set_ylim(0, 1)
    axis.axis("off")
    save(figure, "epochs.png")


def two_directions(fixed):
    groups = [
        (FASTER, [
            ("logging.logger-message-define.x10k", "LoggerMessage"),
            ("ef.detect-changes-10k-tracked.x10", "EF: DetectChanges"),
            ("cache.try-get-miss.x10k", "кэш: промах"),
            ("di.create-scope-dispose.x10k", "DI: новый scope"),
        ]),
        (SLOWER, [
            ("di.first-resolve-depth5.x100", "DI: первый resolve"),
            ("di.validate-on-build100.x100", "DI: ValidateOnBuild"),
            ("logging.factory-create.x100", "новый LoggerFactory"),
            ("ef.new-context.x1k", "EF: новый DbContext"),
        ]),
    ]
    figure, axis = canvas(
        4.9,
        0.62,
        2.72,
        0.34,
        "Прогретый код ускорился, первый вызов замедлился",
        "Изменение времени относительно 7.0, режим стенда. Линии EF Core обрываются на 10.0",
    )
    axis.set_xlim(-0.08, 4.04)
    axis.set_ylim(-52, 100)
    positions = range(5)
    axis.axhline(0, color=AXIS, linewidth=1, zorder=1)
    leader_end = 4.1
    release_right = 4.72
    candidate_right = 5.32
    name_left = 5.45
    for colour, scopes in groups:
        ends = []
        for scope, _ in scopes:
            values = [0.0] + [fixed[scope][key] for key in POINTS]
            known = [(x, y) for x, y in zip(positions, values) if y is not None]
            xs, ys = zip(*known)
            axis.plot(xs, ys, color=colour, linewidth=2, solid_capstyle="round", solid_joinstyle="round", zorder=3)
            axis.plot(xs[-1], ys[-1], "o", color=colour, markersize=7, markeredgecolor=SURFACE, markeredgewidth=1.5, zorder=4)
            ends.append((xs[-1], ys[-1]))
        slots = spread([y for _, y in ends], 8.5)
        for (x, y), slot, (scope, label) in zip(ends, slots, scopes):
            candidate = fixed[scope]["e11"]
            axis.plot([x, leader_end], [y, slot], color=AXIS, linewidth=0.7, clip_on=False, zorder=2)
            axis.text(release_right, slot, percent(fixed[scope]["e10"]), va="center", ha="right", fontsize=LABEL_SIZE, weight="bold", color=INK, clip_on=False)
            axis.text(candidate_right, slot, "нет" if candidate is None else percent(candidate), va="center", ha="right", fontsize=LABEL_SIZE, color=INK_2, clip_on=False)
            axis.text(name_left, slot, label, va="center", ha="left", fontsize=SMALL_SIZE, color=INK_2, clip_on=False)
    axis.text(release_right, 97, "10.0", va="center", ha="right", fontsize=SMALL_SIZE, weight="bold", color=INK, clip_on=False)
    axis.text(candidate_right, 97, "11 RC", va="center", ha="right", fontsize=SMALL_SIZE, color=INK_2, clip_on=False)
    axis.text(0.05, 97, "первое использование: дороже", va="center", ha="left", fontsize=SMALL_SIZE, color=INK_2)
    axis.text(0.05, -48, "прогретые операции: дешевле", va="center", ha="left", fontsize=SMALL_SIZE, color=INK_2)
    axis.set_xticks(list(positions), VERSIONS)
    axis.set_yticks([-40, -20, 0, 20, 40, 60, 80])
    signed_percent_axis(axis, "y")
    axis.spines["left"].set_visible(False)
    recessive_grid(axis, "y")
    save(figure, "two-directions.png")


def load_steps():
    with STEPS_CSV.open(encoding="utf8") as handle:
        return [row for row in csv.DictReader(handle) if row["mode"] == "stand"]


def step_colour(verdict):
    return FASTER if verdict == "faster" else SLOWER if verdict == "slower" else UNCHANGED


def release_steps():
    rows = [
        ("7.0 → 8.0", "первый релиз\nпосле ChatGPT", "e0-7>e1-8"),
        ("8.0 → 9.0", "", "e1-8>e1-9"),
        ("9.0 → 10.0", "первые PR\nагента", "e1-9>e2-10"),
        ("10.0 → 11 RC", "без EF Core", "e2-10>e2-11"),
    ]
    callouts = {
        "e0-7>e1-8": [("logging.logger-message-define.x10k", "LoggerMessage", "left", -11), ("di.first-resolve-depth5.x100", "первый resolve", "right", 1)],
        "e1-8>e1-9": [("di.build1000.x10", "сборка DI на 1000", "left", -1), ("di.validate-on-build100.x100", "ValidateOnBuild", "center", 0)],
        "e1-9>e2-10": [("cache.size-limit-compact-half.x1", "Compact в кэше", "right", 1), ("ef.detect-changes-10k-tracked.x10", "DetectChanges", "left", -1)],
        "e2-10>e2-11": [("di.first-resolve-depth5.x100", "первый resolve", "center", 0), ("config.cold.build-json2000", "сборка JSON", "right", 1)],
    }
    clip = 80
    figure, axis = canvas(
        4.75,
        1.28,
        0.92,
        0.36,
        "Больше всего подтверждённых сдвигов в 8.0",
        "Точка — операция, положение — изменение относительно предыдущего релиза. Цветные\nподтверждены вердиктом, серые нет. Число на полосе — операции внутри ±5%. Режим стенда",
    )
    axis.set_xlim(-50, clip + 8)
    axis.set_ylim(len(rows) - 0.5, -0.62)
    width_inches, height_inches = axis_inches(axis)
    inches_per_unit = width_inches / (clip + 58)
    row_per_inch = (len(rows) - 0.5 + 0.62) / height_inches
    axis.axvspan(-5, 5, color=NOISE_BAND, linewidth=0, zorder=0)
    diameter = 0.098
    results = load_steps()
    for row, (label, note, step) in enumerate(rows):
        selected = [item for item in results if item["step"] == step]
        changes = {item["scope"]: float(item["change_pct"]) for item in selected}
        outside = [item for item in selected if abs(float(item["change_pct"])) >= 5]
        confirmed = sum(item["verdict"] in ("faster", "slower") for item in selected)
        visible = [item for item in outside if abs(float(item["change_pct"])) <= clip]
        values = [float(item["change_pct"]) for item in visible]
        for item, value, offset in zip(visible, values, swarm(values, inches_per_unit, diameter)):
            axis.plot([value], [row + offset * row_per_inch], "o", color=step_colour(item["verdict"]), markersize=6.2, markeredgecolor=SURFACE, markeredgewidth=0.6, zorder=3)
        axis.text(0, row, str(len(selected) - len(outside)), ha="center", va="center", fontsize=LABEL_SIZE, color=INK_2, zorder=4)
        beyond = sorted(float(item["change_pct"]) for item in outside if abs(float(item["change_pct"])) > clip)
        if beyond:
            axis.plot([clip + 5], [row], ">", color=SLOWER, markersize=7, zorder=3)
            axis.text(clip + 7, row + 0.28, ", ".join(percent(value).replace("%", "") for value in beyond), ha="right", va="center", fontsize=SMALL_SIZE, color=INK_2)
        for scope, name, side, shift in callouts[step]:
            value = changes[scope]
            axis.text(value + shift, row - 0.21, f"{name} {percent(value).replace('%', '')}", ha=side, va="bottom", fontsize=SMALL_SIZE, color=INK_2)
        axis.text(-0.195, row - (0.13 if note else 0), label, transform=axis.get_yaxis_transform(), ha="left", va="center", fontsize=LABEL_SIZE, color=INK, weight="bold", clip_on=False)
        if note:
            axis.text(-0.195, row + 0.03, note, transform=axis.get_yaxis_transform(), ha="left", va="top", fontsize=SMALL_SIZE - 1, color=INK_2, linespacing=1.2, clip_on=False)
        axis.text(1.03, row - 0.1, str(confirmed), transform=axis.get_yaxis_transform(), ha="left", va="center", fontsize=17, weight="bold", color=INK, clip_on=False)
        axis.text(1.03, row + 0.2, f"из {len(selected)}", transform=axis.get_yaxis_transform(), ha="left", va="center", fontsize=SMALL_SIZE, color=INK_2, clip_on=False)
    axis.text(1.03, -0.56, "подтверждено", transform=axis.get_yaxis_transform(), ha="left", va="center", fontsize=SMALL_SIZE - 1.5, color=INK_2, clip_on=False)
    axis.set_yticks([])
    axis.set_xticks([-40, -20, 0, 20, 40, 60])
    signed_percent_axis(axis, "x")
    axis.spines["left"].set_visible(False)
    recessive_grid(axis, "x")
    save(figure, "release-steps.png")


def binder_variants():
    rows = [
        ("10.0, пакет", 29, UNCHANGED, "17,2 КБ"),
        ("11 RC3, пакет", 120, SLOWER, "27 КБ"),
        ("11 RC3 + inherit: false", 45, FASTER, "17,2 КБ"),
        ("11 RC3, строка заменена на false", 29, FASTER, "17,2 КБ"),
        ("биндер из main, #135672", 45, FASTER, "17,2 КБ"),
        ("11 RC3, генератор исходников", 12, FASTER, "5,7 КБ"),
    ]
    figure, axis = canvas(
        3.45,
        2.72,
        0.82,
        0.1,
        "Почти вся регрессия биндинга сидит в одной строке",
        "Get<T> для класса на 50 свойств, мкс на вызов. BenchmarkDotNet, без EmberTrace",
    )
    axis.set_xlim(-2, 145)
    axis.set_ylim(len(rows) - 0.5, -0.8)
    for index, (_, value, colour, _) in enumerate(rows):
        rounded_bar(axis, index, value, 0.56, colour)
        axis.text(value + 2, index, f"{value} мкс", va="center", ha="left", fontsize=LABEL_SIZE, color=INK, weight="bold" if colour == SLOWER else "normal")
    for index, (_, _, _, allocated) in enumerate(rows):
        axis.text(1.02, index, allocated, transform=axis.get_yaxis_transform(), va="center", ha="left", fontsize=SMALL_SIZE, color=INK_2, clip_on=False)
    axis.text(1.02, -0.72, "память", transform=axis.get_yaxis_transform(), va="center", ha="left", fontsize=SMALL_SIZE, color=INK_2, clip_on=False)
    axis.axvline(0, color=AXIS, linewidth=1, zorder=3)
    axis.set_yticks(range(len(rows)), [row[0] for row in rows], color=INK, fontsize=LABEL_SIZE - 0.5)
    axis.set_xticks([])
    axis.tick_params(length=0)
    for spine in axis.spines.values():
        spine.set_visible(False)
    save(figure, "binder-variants.png")


def rc11(fixed):
    rows = [
        ("options.snapshot-new-scope.x1k", "IOptionsSnapshot в новом scope"),
        ("config.bind-poco50.x1k", "биндинг POCO на 50 полей"),
        ("config.cold.build-json2000", "холодная сборка конфигурации"),
        ("options.validate-data-annotations.x1k", "валидация DataAnnotations"),
        ("cache.cold.create-and-first-set", "холодный старт MemoryCache"),
    ]
    figure, axis = canvas(
        3.75,
        2.5,
        0.1,
        0.1,
        "В 11 RC биндинг и снапшот опций подорожали в 2,4–2,6 раза",
        "Изменение времени относительно 7.0, режим стенда",
    )
    axis.set_xlim(-6, 196)
    axis.set_ylim(len(rows) - 0.5, -0.5)
    thickness = 0.3
    for index, (scope, _) in enumerate(rows):
        for shift, key, colour, weight in ((-0.17, "e10", UNCHANGED, "normal"), (0.17, "e11", SLOWER, "bold")):
            value = fixed[scope][key]
            rounded_bar(axis, index + shift, value, thickness, colour)
            axis.text(max(value, 0) + 2.5, index + shift, percent(value), va="center", ha="left", fontsize=SMALL_SIZE, color=INK, weight=weight)
    axis.axvline(0, color=AXIS, linewidth=1, zorder=3)
    axis.set_yticks(range(len(rows)), [label for _, label in rows], color=INK, fontsize=LABEL_SIZE)
    axis.set_xticks([])
    axis.tick_params(length=0)
    for spine in axis.spines.values():
        spine.set_visible(False)
    axis.legend(
        handles=[Patch(color=UNCHANGED, label="10.0"), Patch(color=SLOWER, label="11.0 RC3")],
        loc="lower right",
        fontsize=LABEL_SIZE,
        labelcolor=INK_2,
        handlelength=1.1,
    )
    save(figure, "rc11.png")


def allocations(bytes_change):
    labels = {
        "ef.detect-changes-10k-tracked.x10": "EF: DetectChanges",
        "logging.logger-message-define.x10k": "LoggerMessage.Define",
        "cache.size-limit-compact-half.x1": "кэш: Compact при лимите",
        "config.bind-poco50.x1k": "биндинг POCO на 50 полей",
        "options.snapshot-new-scope.x1k": "IOptionsSnapshot в новом scope",
        "logging.factory-create.x100": "новый LoggerFactory",
        "ef.new-context.x1k": "EF: новый DbContext",
        "di.build100.x100": "DI: сборка, 100 регистраций",
        "di.build1000.x10": "DI: сборка, 1000 регистраций",
        "logging.cold.first-log": "первая запись в лог",
        "ef.cold.first-query": "EF: первый запрос",
        "ef.query-where-orderby-take.x1k": "EF: Where + OrderBy + Take",
        "di.validate-on-build100.x100": "DI: сборка с ValidateOnBuild",
        "ef.to-query-string.x1k": "EF: ToQueryString",
    }
    rows = sorted(((bytes_change[scope], label) for scope, label in labels.items()), key=lambda row: row[0])
    figure, axis = canvas(
        4.75,
        2.42,
        0.1,
        0.08,
        "Аллокации: от −42% у DetectChanges до +51% у ToQueryString",
        "Байт на пачку вызовов, 10.0 против 7.0. Показаны крупные изменения и биндинг\nс IOptionsSnapshot; у остальных операций сдвиг меньше 10%",
    )
    axis.set_xlim(-56, 62)
    axis.set_ylim(len(rows) - 0.5, -0.5)
    for index, (value, _) in enumerate(rows):
        rounded_bar(axis, index, value, 0.62, FASTER if value < 0 else SLOWER)
        anchor = value + (1.2 if value > 0 else -1.2)
        axis.text(anchor, index, percent(value), va="center", ha="left" if value > 0 else "right", fontsize=SMALL_SIZE, color=INK)
    axis.axvline(0, color=AXIS, linewidth=1, zorder=3)
    axis.set_yticks(range(len(rows)), [label for _, label in rows], color=INK, fontsize=LABEL_SIZE - 0.5)
    axis.set_xticks([])
    axis.tick_params(length=0)
    for spine in axis.spines.values():
        spine.set_visible(False)
    save(figure, "allocations.png")


def modes(fixed, stock):
    labels = [
        ("di.singleton.x10k", "DI: singleton"),
        ("di.open-generic.x10k", "DI: открытый generic"),
        ("di.transient.x10k", "DI: transient"),
        ("di.first-resolve-depth5.x100", "DI: первый resolve"),
        ("options.monitor-current-value.x10k", "IOptionsMonitor.CurrentValue"),
        ("logging.factory-create.x100", "новый LoggerFactory"),
        ("logging.logger-message-define.x10k", "LoggerMessage.Define"),
        ("cache.try-get-hit.x10k", "кэш: TryGetValue, попадание"),
    ]
    rows = sorted(labels, key=lambda row: -abs(fixed[row[0]]["e10"] - stock[row[0]]["e10"]))
    figure, axis = canvas(
        4.5,
        2.28,
        0.72,
        0.34,
        "DI и IOptionsMonitor: режим питания меняет результат на 9–14 п.п.",
        "Изменение времени, 10.0 против 7.0. Серая полоса — порог шума ±5%",
        reserve=0.28,
    )
    for index, (scope, _) in enumerate(rows):
        left, right = fixed[scope]["e10"], stock[scope]["e10"]
        axis.plot([left, right], [index, index], color=AXIS, linewidth=2, zorder=2, solid_capstyle="butt")
        axis.plot([left], [index], "o", color=STAND, markersize=8.5, markeredgecolor=SURFACE, markeredgewidth=1.5, zorder=3)
        axis.plot([right], [index], "D", color=STOCK, markersize=7.5, markeredgecolor=SURFACE, markeredgewidth=1.5, zorder=3)
        axis.text(1.02, index, f"{abs(left - right):.0f} п.п.", transform=axis.get_yaxis_transform(), va="center", ha="left", fontsize=LABEL_SIZE, color=INK, weight="bold", clip_on=False)
    axis.axvspan(-5, 5, color=NOISE_BAND, linewidth=0, zorder=0)
    axis.axvline(0, color=AXIS, linewidth=1, zorder=1)
    axis.set_yticks(range(len(rows)), [label for _, label in rows], color=INK, fontsize=LABEL_SIZE - 0.5)
    axis.set_ylim(len(rows) - 0.5, -0.6)
    axis.text(1.02, 1.02, "разница", transform=axis.transAxes, va="bottom", ha="left", fontsize=SMALL_SIZE, color=INK_2, clip_on=False)
    axis.set_xlim(-45, 68)
    axis.set_xticks([-40, -20, 0, 20, 40, 60])
    signed_percent_axis(axis, "x")
    axis.spines["left"].set_visible(False)
    axis.legend(
        handles=[
            Line2D([], [], marker="o", linestyle="", color=STAND, markersize=8.5, label="стенд: 2 ГГц, без турбо"),
            Line2D([], [], marker="D", linestyle="", color=STOCK, markersize=7.5, label="заводские: powersave, турбо"),
        ],
        loc="lower left",
        ncol=2,
        fontsize=SMALL_SIZE,
        labelcolor=INK_2,
        bbox_to_anchor=(-0.02, 1.0),
        handletextpad=0.25,
        columnspacing=1.0,
        borderpad=0,
    )
    recessive_grid(axis, "x")
    save(figure, "modes.png")


def main():
    fixed, stock, bytes_change = load()
    cover(fixed)
    epochs()
    two_directions(fixed)
    release_steps()
    binder_variants()
    rc11(fixed)
    allocations(bytes_change)
    modes(fixed, stock)


if __name__ == "__main__":
    main()
