#!/usr/bin/env bash
# 构建并发布 use-ai-rule 生成器（Linux/mac 入口，供 CI 与非 Windows 本机使用）。
# 同时打包 merged 内容包（fragments + skills，manifest.vars.version 作统一版本）：
#   dist/ai-rule-pack.zip             —— 内容包（pack.json + manifest.json + fragments/ + skills/）
#   dist/latest.json                  —— 更新检查清单（统一版本 + exe / zip SHA-256），随 Release 上传
# 统一版本口径：exe 与内容包共用 manifest.vars.version，exe 侧经 InformationalVersion 注入；
# 交互启动时拉 latest.json 一次，统一版本同时对照 exe 与生效内容两个基准，发现更新弹确认（默认更新），下载校验后落位；无网络回退内嵌资源。
# 用法：
#   ./publish.sh [rid ...]                         默认 win-x64 linux-x64 osx-x64
#   ./publish.sh --framework-dependent [rid ...]   退回框架相关发布（目标机需自装 .NET 10 运行时）
#   ./publish.sh --skip-pack [rid ...]             只发布 exe，不重打内容包
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$here"

rids=()
framework_dependent=false
skip_pack=false
for arg in "$@"; do
  case "$arg" in
    --framework-dependent) framework_dependent=true ;;
    --skip-pack) skip_pack=true ;;
    *) rids+=("$arg") ;;
  esac
done
if [ ${#rids[@]} -eq 0 ]; then
  rids=(win-x64 linux-x64 osx-x64)
fi

repo_root="$(cd "$here/../.." && pwd)"
dist_directory="$repo_root/dist"
mkdir -p "$dist_directory"

# 统一版本：exe 与内容包共用 manifest.vars.version，构建前先读出以注入 InformationalVersion
version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$repo_root/fragments/manifest.json" | head -1)"
if [ -z "$version" ]; then
  echo "manifest.vars.version 为空，无法确定发布版本" >&2
  exit 1
fi
publish_args=()
if $framework_dependent; then
  publish_args+=(--self-contained false)
else
  publish_args+=(--self-contained true)
fi

published=()
for rid in "${rids[@]}"; do
  echo ""
  echo "发布 $rid（单文件，self-contained，版本 $version）..."
  dotnet publish -c Release -r "$rid" "${publish_args[@]}" \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:IncludeSourceRevisionInInformationalVersion=false \
    -p:InformationalVersion="$version"

  binary_name="use-ai-rule"
  case "$rid" in
    win-*)
      binary_name="use-ai-rule.exe"
      ;;
  esac
  published_binary="$here/bin/Release/net10.0/$rid/publish/$binary_name"
  if [ ! -f "$published_binary" ]; then
    echo "发布完成但未找到产物：$published_binary" >&2
    exit 1
  fi

  # Windows 产物固定为 use-ai-rule.exe，非 Windows 带 RID 后缀
  case "$rid" in
    win-*)
      dist_binary="$dist_directory/use-ai-rule.exe"
      ;;
    *)
      dist_binary="$dist_directory/use-ai-rule-$rid"
      ;;
  esac
  cp -f "$published_binary" "$dist_binary"
  published+=("$dist_binary")
done

# ── merged 内容包：fragments + skills 打 zip，pack.json 记录版本与逐文件 SHA-256 ──
if [ "$skip_pack" = false ]; then

  stage_directory="$repo_root/dist/pack-stage"
  rm -rf "$stage_directory"
  mkdir -p "$stage_directory"
  cp -r "$repo_root/fragments" "$stage_directory/fragments"
  cp -r "$repo_root/skills" "$stage_directory/skills"
  rm -f "$stage_directory/skills/README.md"

  # 逐文件 SHA-256 清单（相对路径用 /，哈希小写十六进制）
  python3 - "$stage_directory" "$version" <<'PYEOF'
import hashlib, json, os, sys

stage, version = sys.argv[1], sys.argv[2]
files = {}
for base, _, names in os.walk(stage):
    for name in names:
        if name == "pack.json":
            continue
        full = os.path.join(base, name)
        relative = os.path.relpath(full, stage).replace(os.sep, "/")
        with open(full, "rb") as handle:
            files[relative] = hashlib.sha256(handle.read()).hexdigest()
manifest = {"kind": "merged", "version": version, "schema_version": 1, "files": dict(sorted(files.items()))}
with open(os.path.join(stage, "pack.json"), "w", encoding="utf-8") as handle:
    json.dump(manifest, handle, ensure_ascii=False, indent=2)
PYEOF

  pack_zip="$dist_directory/ai-rule-pack.zip"
  rm -f "$pack_zip"
  (cd "$stage_directory" && zip -q -r -X "$pack_zip" .)
  win_exe_sha256="$(sha256sum "$dist_directory/use-ai-rule.exe" | cut -d' ' -f1)"
  zip_sha256="$(sha256sum "$pack_zip" | cut -d' ' -f1)"
  # engine_version 是旧版 exe（2.x 引擎轨道）的兼容字段，写同值助其一次性迁移到新 exe
  printf '{\n  "version": "%s",\n  "engine_version": "%s",\n  "engine_sha256": "%s",\n  "sha256": "%s"\n}\n' "$version" "$version" "$win_exe_sha256" "$zip_sha256" > "$dist_directory/latest.json"

  rm -rf "$stage_directory"
  echo ""
  echo "内容包完成：$pack_zip（版本 $version）"
fi

echo ""
echo "发布完成："
for item in "${published[@]}"; do
  echo "  $item"
done
