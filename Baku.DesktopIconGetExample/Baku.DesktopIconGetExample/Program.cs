using System;

namespace Baku.DesktopIconGetExample
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var getter = new DesktopIconGetter())
            {
                foreach(var icon in getter.GetAllIconInfo())
                {
                    Console.WriteLine(
                        $"{icon.Name}: L={icon.Left}, T={icon.Top}, W={icon.Width}, H={icon.Height}"
                        );
                }
            }
        }
    }
}
