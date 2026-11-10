namespace IraqPlatformer;

internal readonly record struct MovingPlatform(Rectangle Origin, int TravelX, int TravelY, int Phase)
{
    public Rectangle At(int tick)
    {
        double wave = Math.Sin((tick + Phase) * .021);
        Rectangle bounds = Origin;
        bounds.Offset((int)(TravelX * wave), (int)(TravelY * wave));
        return bounds;
    }
}

internal sealed record StageLayout(Rectangle[] Platforms, MovingPlatform[] Moving, Rectangle SecretLedge, int Flag, int Gate, int Width)
{
    public static StageLayout Build(Stage stage)
    {
        if (stage.Province == 1)
            return stage.Number == 1 ? BaghdadMarket() : BaghdadRoute(stage.Number);
        if (stage.Province == 19) return HalabjaValley(stage.Number);
        const int ground = 440;
        int sections = stage.Number switch { 1 => 6, <= 4 => 8, <= 7 => 10, _ => 12 };
        var platforms = new List<Rectangle> { new(0, ground, 320, 80) };
        var moving = new List<MovingPlatform>();
        bool water = stage.Province is 1 or 4 or 5 or 7 or 8 or 10 or 11 or 13 or 14 or 15;
        var random = new Random(stage.Province * 100 + stage.Number);
        int edge = 320;
        int flag = 0;
        Rectangle secret = Rectangle.Empty;
        for (int section = 0; section < sections; section++)
        {
            int gap = random.Next(48, 92 + stage.Number * 5);
            int left = edge + gap;
            int width = random.Next(530, 770);
            if (stage.Boss is not null && section == sections - 1)
                width = Math.Max(width, 640);
            platforms.Add(new Rectangle(left, ground, width, 80));

            int ledge = left + random.Next(80, 175);
            int top = ground - random.Next(65, 107);
            platforms.Add(new Rectangle(ledge, top, random.Next(85, 140), 18));
            if (stage is { Province: 5, Number: 3 } || (section + stage.Province + stage.Number) % 3 == 0)
                platforms.Add(new Rectangle(ledge + 110, top - 47, 92, 18));

            if (stage.Number >= 2 && section % 3 == 1)
            {
                if (water)
                    moving.Add(new MovingPlatform(new Rectangle(edge + (gap - 64) / 2, 458, 64, 14), gap / 4, 12, section * 51));
                else
                    moving.Add(new MovingPlatform(new Rectangle(left + 365, 359, 74, 16), 0, 49, section * 51));
            }

            if (section == sections / 2 + 1)
            {
                platforms.Add(new Rectangle(left + width - 300, 365, 72, 16));
                secret = new Rectangle(left + width - 213, 297, 108, 16);
                platforms.Add(secret);
            }

            if (section == sections / 2)
                flag = left + 35;
            edge = left + width;
        }

        return new StageLayout(platforms.ToArray(), moving.ToArray(), secret, flag, edge - 125, edge);
    }

    private static StageLayout HalabjaValley(int number)
    {
        int[] gaps = number switch
        {
            1 => [52, 60, 72, 76, 70, 62],
            2 => [62, 85, 79, 89, 72, 92, 67],
            3 => [69, 82, 105, 90, 109, 73, 94],
            4 => [60, 93, 107, 83, 115, 95, 103, 69],
            5 => [78, 102, 85, 117, 91, 118, 96, 81],
            6 => [77, 97, 111, 92, 116, 103, 88, 119, 91],
            7 => [81, 109, 100, 122, 94, 107, 124, 87, 107],
            _ => [93, 101, 112, 95, 117, 86, 125, 98, 114, 103]
        };
        var platforms = new List<Rectangle> { new(0, 440, 560, 80) };
        var moving = new List<MovingPlatform>();
        int edge = 560, flag = 0;
        Rectangle secret = Rectangle.Empty;
        for (int i = 0; i < gaps.Length; i++)
        {
            int left = edge + gaps[i];
            bool last = i == gaps.Length - 1;
            int width = last && number == 8 ? 980 : 555 + 42 * ((i * 3 + number) % 5);
            platforms.Add(new Rectangle(left, 440, width, 80));

            if (!(last && number == 8))
            {
                int rise = 55 + 10 * ((i + number) % 4);
                platforms.Add(new Rectangle(left + 106, 440 - rise, 115, 17));
                if (number >= 3 && i % 2 == 0)
                    platforms.Add(new Rectangle(left + 235, 387 - rise, 104, 16));
            }

            if (number is 3 or 5 or 7 && i % 2 == 0)
                moving.Add(new MovingPlatform(new Rectangle(edge + gaps[i] / 2 - 35, 453, 70, 15),
                    number == 5 ? 21 : 12, number == 3 ? 21 : 11, i * 35));
            if (i == gaps.Length / 2)
                flag = left + 32;
            if (i == gaps.Length / 2 + 1)
            {
                platforms.Add(new Rectangle(left + width - 315, 366, 81, 16));
                secret = new Rectangle(left + width - 216, 302, 113, 16);
                platforms.Add(secret);
            }
            edge = left + width;
        }
        return new StageLayout(platforms.ToArray(), moving.ToArray(), secret, flag, edge - 125, edge);
    }

    private static StageLayout BaghdadRoute(int number)
    {
        // Section widths and the gap before each section, tuned for each Baghdad route.
        (int[] Widths, int[] Gaps) route = number switch
        {
            2 => ([620, 930, 510, 740, 880, 560, 980], [0, 48, 56, 46, 70, 52, 64]),
            3 => ([590, 740, 880, 590, 720, 840, 990], [0, 62, 74, 70, 55, 91, 77]),
            4 => ([680, 615, 530, 610, 680, 560, 605, 850], [0, 85, 126, 94, 135, 120, 104, 78]),
            5 => ([640, 550, 700, 550, 640, 570, 540, 630, 900], [0, 81, 87, 104, 73, 118, 98, 110, 72]),
            6 => ([630, 590, 590, 700, 555, 620, 550, 680, 940], [0, 92, 109, 83, 114, 126, 93, 112, 104]),
            7 => ([610, 630, 730, 530, 690, 570, 680, 680, 990], [0, 68, 96, 105, 94, 115, 120, 101, 98]),
            _ => ([610, 680, 530, 760, 620, 575, 650, 610, 725, 1050], [0, 83, 92, 96, 102, 124, 94, 110, 89, 104])
        };
        var platforms = new List<Rectangle>();
        var moving = new List<MovingPlatform>();
        int edge = 0, flag = 0;
        Rectangle secret = Rectangle.Empty;
        for (int i = 0; i < route.Widths.Length; i++)
        {
            int gap = route.Gaps[i], width = route.Widths[i], left = edge + gap;
            platforms.Add(new Rectangle(left, 440, width, 80));
            if (number == 4 && i > 0 && i % 2 == 0)
                moving.Add(new MovingPlatform(new Rectangle(edge + gap / 2 - 35, 454, 70, 15), 24, 10, i * 30));
            if (number == 5 && i > 0)
                moving.Add(new MovingPlatform(new Rectangle(left + 350, 353, 92, 16), 0, 55, i * 42));

            bool arena = number == 8 && i == route.Widths.Length - 1;
            if (!arena)
            {
                int top = number == 6 ? 359 : 372;
                platforms.Add(new Rectangle(left + 135, top, number == 2 ? 225 : 115, 18));
                if (number is 3 or 6 or 8)
                    platforms.Add(new Rectangle(left + 275, top - 55, 105, 16));
            }

            if (i == route.Widths.Length / 2)
                flag = left + 25;
            if (i == route.Widths.Length / 2 + 1)
            {
                platforms.Add(new Rectangle(left + width - 190, 362, 79, 18));
                secret = new Rectangle(left + width - 105, 297, 98, 17);
                platforms.Add(secret);
            }
            edge = left + width;
        }

        return new StageLayout(platforms.ToArray(), moving.ToArray(), secret, flag, edge - 125, edge);
    }

    private static StageLayout BaghdadMarket()
    {
        Rectangle roof = new(3420, 290, 148, 17);
        Rectangle[] platforms =
        [
            new(0, 440, 910, 80),

            new(170, 367, 101, 18),
            new(300, 321, 119, 18),
            new(970, 440, 850, 80),
            new(1090, 374, 139, 18),
            new(1260, 319, 136, 18),
            new(1902, 440, 700, 80),
            new(2090, 365, 128, 20),
            new(2695, 440, 980, 80),
            new(3080, 374, 94, 18),
            new(3216, 331, 118, 17),
            roof,
            new(3774, 440, 780, 80),
            new(3950, 374, 130, 18),
            new(4105, 327, 140, 18),
            new(4652, 440, 825, 80),
            new(4852, 376, 107, 16),
            new(4974, 337, 106, 16),
            new(5097, 296, 115, 16)
        ];
        return new StageLayout(platforms, [], roof, 2725, 5352, 5477);
    }
}
