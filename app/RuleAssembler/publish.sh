#!/usr/bin/env bash
# 构建并发布 use-ai-rule 生成器（Linux/mac 入口，供 CI 与非 Windows 本机使用）。
# exe 单产物口径，规则与技能内容全量打进 exe：
#   dist/use-ai-rule[-<rid>][.exe]    —— 单文件产物（win-x64 默认框架依赖；目标机需自装 .NET 10 运行时）
#   dist/latest.json                  —— 更新检查清单（统一版本 + win exe SHA-256），随 Release 上传
# 统一版本口径：exe 内嵌内容的 manifest.vars.version 与 exe 的 InformationalVersion 同源注入；
# 交互启动时拉 latest.json 一次对照 exe 版本，发现更新弹确认（默认更新），同机共享缓存命中或下载校验替换；无网络回退内嵌资源。
# 用法：
#   ./publish.sh [rid ...]                         默认 win-x64 linux-x64 osx-x64
#   ./publish.sh --framework-dependent [rid ...]   退回框架相关发布（目标机需自装 .NET 10 运行时）
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$here"

rids=()
framework_dependent=false
for arg in "$@"; do
  case "$arg" in
    --framework-dependent) framework_dependent=true ;;
    *) rids+=("$arg") ;;
  esac
done
if [ ${#rids[@]} -eq 0 ]; then
  rids=(win-x64 linux-x64 osx-x64)
fi

repo_root="$(cd "$here/../.." && pwd)"
dist_directory="$repo_root/dist"
mkdir -p "$dist_directory"

# 统一版本：exe 内嵌内容与 InformationalVersion 共用 manifest.vars.version，构建前先读出以注入
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

# latest.json（统一版本）：version = exe 注入版本；
# engine_sha256 供确认后自更新做下载完整性校验（缺省则跳过引擎替换），取 win-x64 产物哈希；
# engine_version 是旧版 exe（2.x 引擎轨道）的兼容字段，写同值助其一次性迁移到新 exe
win_exe="$dist_directory/use-ai-rule.exe"
if [ -f "$win_exe" ]; then
  win_exe_sha256="$(sha256sum "$win_exe" | cut -d' ' -f1)"
  printf '{\n  "version": "%s",\n  "engine_version": "%s",\n  "engine_sha256": "%s"\n}\n' "$version" "$version" "$win_exe_sha256" > "$dist_directory/latest.json"
fi

echo ""
echo "发布完成："
for item in "${published[@]}"; do
  echo "  $item"
done
