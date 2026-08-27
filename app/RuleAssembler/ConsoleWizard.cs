namespace RuleAssembler;

internal sealed class ConsoleWizard
{
    public Profile? SelectProfile(Manifest manifest)
    {
        while (true)
        {
            Console.WriteLine("请选择技术大类：");
            for (var i = 0; i < manifest.Families.Count; i++)
                Console.WriteLine($"  {i + 1}. {manifest.Families[i].Label} — {manifest.Families[i].Desc}");
            Console.Write($"输入编号 (1-{manifest.Families.Count})，0 取消：");

            var familyIndex = ReadIndex(manifest.Families.Count);
            if (familyIndex is null)
                return null;

            var family = manifest.Families[familyIndex.Value];
            var profiles = manifest.Profiles
                .Where(profile => profile.Family.Equals(family.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (profiles.Count == 0)
            {
                Console.WriteLine("该大类下暂无已接入 Profile，请重新选择。\n");
                continue;
            }

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine($"请选择 {family.Label} Profile：");
                for (var i = 0; i < profiles.Count; i++)
                    Console.WriteLine($"  {i + 1}. {profiles[i].Label} — {profiles[i].Desc}");
                Console.Write($"输入编号 (1-{profiles.Count})，0 返回上一级：");

                var profileIndex = ReadIndex(profiles.Count);
                if (profileIndex is null)
                {
                    Console.WriteLine();
                    break;
                }

                return profiles[profileIndex.Value];
            }
        }
    }

    private static int? ReadIndex(int count)
    {
        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            if (input is null || input == "0")
                return null;
            if (int.TryParse(input, out var index) && index >= 1 && index <= count)
                return index - 1;

            Console.Write($"输入无效，请输入 1-{count}，0 取消：");
        }
    }
}
