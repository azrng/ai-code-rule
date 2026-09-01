using System.Text;

namespace RuleAssembler;

internal sealed class OutputWriter
{
    public int WriteFiles(IReadOnlyList<PlannedFile> files)
    {
        return WriteFiles(Environment.CurrentDirectory, files);
    }

    internal int WriteFiles(string rootDirectory, IReadOnlyList<PlannedFile> files)
    {
        var cwd = Path.GetFullPath(rootDirectory);
        var existing = files.Where(file => File.Exists(PathGuard.GetPathWithin(cwd, file.Output))).ToList();
        foreach (var file in files)
        {
            var path = PathGuard.GetPathWithin(cwd, file.Output);
            if (Directory.Exists(path))
                throw new IOException($"目标路径是目录，无法写入文件：{file.Output}");
        }

        if (existing.Count > 0)
            Console.WriteLine($"发现 {existing.Count} 个同名文件，将直接覆盖。");

        var staging = Path.Combine(cwd, $".ruleassembler-staging-{Guid.NewGuid():N}");
        var backup = Path.Combine(staging, "backup");
        var committed = new List<CommitRecord>();

        try
        {
            Directory.CreateDirectory(staging);
            foreach (var file in files)
            {
                var stagedPath = GetStagedPath(staging, file.Output);
                var directory = Path.GetDirectoryName(stagedPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(stagedPath, file.Content, new UTF8Encoding(false));
            }

            foreach (var file in files)
            {
                var targetPath = PathGuard.GetPathWithin(cwd, file.Output);
                var stagedPath = GetStagedPath(staging, file.Output);
                var existed = File.Exists(targetPath);
                var backupPath = existed ? GetStagedPath(backup, file.Output) : "";
                var record = new CommitRecord(targetPath, backupPath, existed);
                committed.Add(record);

                if (existed)
                {
                    var backupDirectory = Path.GetDirectoryName(backupPath);
                    if (!string.IsNullOrEmpty(backupDirectory))
                        Directory.CreateDirectory(backupDirectory);
                    File.Move(targetPath, backupPath);
                }

                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);
                File.Move(stagedPath, targetPath);
                record.Installed = true;
                Console.WriteLine($"  写入 {file.Output}");
            }

            return files.Count;
        }
        catch (Exception ex)
        {
            Rollback(committed);
            throw new IOException("释放过程中发生错误，已回滚本次已写入的文件。", ex);
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                try
                {
                    Directory.Delete(staging, recursive: true);
                }
                catch (IOException)
                {
                    Console.Error.WriteLine($"提示：临时目录未能清理，请稍后删除：{staging}");
                }
            }
        }
    }

    private static void Rollback(IEnumerable<CommitRecord> records)
    {
        foreach (var record in records.Reverse())
        {
            if (record.Installed && File.Exists(record.TargetPath))
                File.Delete(record.TargetPath);
            if (record.Existed && File.Exists(record.BackupPath))
            {
                var directory = Path.GetDirectoryName(record.TargetPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.Move(record.BackupPath, record.TargetPath);
            }
        }
    }

    private static string GetStagedPath(string staging, string output)
    {
        return PathGuard.GetPathWithin(staging, output);
    }

    private sealed class CommitRecord
    {
        public CommitRecord(string targetPath, string backupPath, bool existed)
        {
            TargetPath = targetPath;
            BackupPath = backupPath;
            Existed = existed;
        }

        public string TargetPath { get; }
        public string BackupPath { get; }
        public bool Existed { get; }
        public bool Installed { get; set; }
    }
}
