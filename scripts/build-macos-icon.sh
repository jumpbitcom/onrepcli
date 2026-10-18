#!/usr/bin/env bash
# Builds assets/jumpbit-app.icns from assets/jumpbit-app-1024.png using the macOS icon grid:
# the artwork is scaled to an 824 px body centred on a transparent 1024 px canvas with a soft
# shadow, so the Dock icon matches the size of other macOS apps.
#
# Usage: scripts/build-macos-icon.sh [-h|--help]
# Requires macOS with swift, sips and iconutil. Exit codes: 0 success, 1 error, 2 usage error.
set -Eeuo pipefail
shopt -s nullglob
# inherit_errexit needs Bash 4.4+; this macOS-only script must also run on the system Bash 3.2.
shopt -s inherit_errexit 2> /dev/null || true
IFS=$'\n\t'

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly SOURCE_PNG="${script_dir}/../assets/jumpbit-app-1024.png"
readonly TARGET_ICNS="${script_dir}/../assets/jumpbit-app.icns"
readonly SIZES=(16 32 128 256 512)
work_dir=""

usage() {
  printf 'Usage: %s [-h|--help]\n' "$(basename -- "$0")"
  printf 'Regenerates assets/jumpbit-app.icns with macOS icon padding.\n'
}

cleanup() {
  if [[ -n "${work_dir}" ]]; then
    rm -rf -- "${work_dir:?}"
  fi
}

require_tools() {
  local tool
  for tool in swift sips iconutil; do
    if ! command -v "${tool}" > /dev/null; then
      printf 'Error: %s is required (macOS only).\n' "${tool}" >&2
      exit 1
    fi
  done
}

write_pad_script() {
  cat > "$1" << 'SWIFT'
import AppKit

let arguments = CommandLine.arguments
guard arguments.count == 3, let artwork = NSImage(contentsOfFile: arguments[1]) else {
    FileHandle.standardError.write("usage: pad.swift <source.png> <target.png>\n".data(using: .utf8)!)
    exit(1)
}

let canvas = 1024
let body: CGFloat = 824
let inset = (CGFloat(canvas) - body) / 2
guard let bitmap = NSBitmapImageRep(
    bitmapDataPlanes: nil, pixelsWide: canvas, pixelsHigh: canvas, bitsPerSample: 8,
    samplesPerPixel: 4, hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB,
    bytesPerRow: 0, bitsPerPixel: 0) else { exit(1) }

NSGraphicsContext.saveGraphicsState()
NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: bitmap)
let shadow = NSShadow()
shadow.shadowColor = NSColor.black.withAlphaComponent(0.3)
shadow.shadowOffset = NSSize(width: 0, height: -10)
shadow.shadowBlurRadius = 20
shadow.set()
artwork.draw(in: NSRect(x: inset, y: inset, width: body, height: body))
NSGraphicsContext.restoreGraphicsState()

guard let png = bitmap.representation(using: .png, properties: [:]) else { exit(1) }
try png.write(to: URL(fileURLWithPath: arguments[2]))
SWIFT
}

main() {
  case "${1:-}" in
    -h | --help)
      usage
      return 0
      ;;
    "") ;;
    *)
      usage >&2
      exit 2
      ;;
  esac

  require_tools
  work_dir="$(mktemp -d)"
  trap cleanup EXIT

  local padded_png="${work_dir}/padded-1024.png"
  local iconset="${work_dir}/jumpbit-app.iconset"
  local size double

  write_pad_script "${work_dir}/pad.swift"
  swift "${work_dir}/pad.swift" "${SOURCE_PNG}" "${padded_png}"

  mkdir -p -- "${iconset}"
  for size in "${SIZES[@]}"; do
    double=$((size * 2))
    sips -z "${size}" "${size}" "${padded_png}" --out "${iconset}/icon_${size}x${size}.png" > /dev/null
    sips -z "${double}" "${double}" "${padded_png}" --out "${iconset}/icon_${size}x${size}@2x.png" > /dev/null
  done

  iconutil -c icns "${iconset}" -o "${TARGET_ICNS}"
  printf 'Wrote %s\n' "${TARGET_ICNS}"
}

main "$@"
