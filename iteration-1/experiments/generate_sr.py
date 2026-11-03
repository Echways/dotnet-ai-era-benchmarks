#!/usr/bin/env python3
import sys
import xml.etree.ElementTree as ElementTree

resources, target = sys.argv[1:3]
items = [(data.get("name"), data.find("value").text) for data in ElementTree.parse(resources).getroot().findall("data")]


def escape(value):
    return value.replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n")


constants = "".join(f'    internal const string {name} = "{escape(value)}";\n' for name, value in items)
with open(target, "w", encoding="utf-8") as output:
    output.write("namespace System;\ninternal static class SR\n{\n" + constants)
    output.write("    internal static string Format(string format, params object?[] arguments) => string.Format(format, arguments);\n}\n")
