#!/usr/bin/env bash
# 构建并发布 use-ai-rule 生成器（Linux/mac 入口，供 CI 与非 Windows 本机使用）。
# RID 语义与产物命名和同目录 publish.ps1 保持一致：
#   dist/use-ai-rule.exe（win）/ dist/use-ai-rule-linux-x64 / dist/use-ai-rule-osx-x64
# Windows 产物统一命名 use-ai-rule.exe，不再额外产出 use-ai-rule-win-x64.exe 等带 RID 后缀的副本。
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

publish_args=()
if $framework_dependent; then
  publish_args+=(--self-contained false)
else
  publish_args+=(--self-contained true)
fi

published=()
for rid in "${rids[@]}"; do
  echo ""
  echo "发布 $rid（单文件，self-contained）..."
  dotnet publish -c Release -r "$rid" "${publish_args[@]}" \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true

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

echo ""
echo "发布完成："
for item in "${published[@]}"; do
  echo "  $item"
done
