using System.Drawing.Drawing2D;

namespace IraqPlatformer;

internal static class BaghdadPreviewArt
{
    private static readonly Lazy<Bitmap> Map = new(() => Load("maps-01-06"));
    private static readonly Lazy<Bitmap> MiddleMap = new(() => Load("maps-07-12"));
    private static readonly Lazy<Bitmap> LastMap = new(() => Load("maps-13-18"));
    private static readonly Lazy<Bitmap> Hero = new(() => Load("HamoudiReference"));
    private static readonly Point[] Silhouette =
    [
        new(19,279), new(58,222), new(79,177), new(120,150), new(122,115),
        new(165,91), new(221,91), new(257,106), new(278,135), new(283,175),
        new(287,183), new(285,220), new(273,254), new(257,275), new(274,295),
        new(294,355), new(311,366), new(324,400), new(347,418), new(352,443),
        new(335,467), new(287,470), new(282,515), new(303,590), new(314,665),
        new(287,679), new(281,701), new(314,719), new(335,708), new(341,733),
        new(324,743), new(244,741), new(235,723), new(244,690), new(179,696),
        new(111,678), new(102,704), new(107,727), new(91,746), new(51,757),
        new(39,748), new(42,730), new(63,699), new(66,672), new(40,657),
        new(37,642), new(54,604), new(85,549), new(119,471), new(118,446),
        new(73,483), new(59,487), new(48,459), new(36,441), new(33,421),
        new(38,400), new(19,380), new(29,350), new(25,329), new(16,314)
    ];

    private static readonly Point[] Pasha =
    [
        new(393,231), new(424,87), new(454,75), new(479,67), new(521,65),
        new(542,69), new(562,96), new(583,124), new(584,151), new(606,164),
        new(622,171), new(618,184), new(597,190), new(578,188), new(579,216),
        new(560,235), new(584,243), new(607,245), new(637,264), new(662,299),
        new(684,325), new(694,350), new(687,388), new(671,433), new(676,470),
        new(685,535), new(692,589), new(699,636), new(676,652), new(655,654),
        new(645,693), new(651,709), new(675,719), new(685,735), new(666,742),
        new(624,743), new(579,731), new(578,711), new(577,694), new(525,710),
        new(482,704), new(463,724), new(453,741), new(416,755), new(398,743),
        new(400,727), new(414,706), new(417,677), new(381,652), new(367,634),
        new(395,562), new(407,513), new(414,472), new(418,432), new(398,425),
        new(387,406), new(394,362), new(407,317), new(426,275), new(469,248),
        new(481,236), new(466,213), new(449,189), new(444,165), new(435,231),
        new(425,243)
    ];

    private static Bitmap Load(string name)
    {
        using Stream stream = typeof(BaghdadPreviewArt).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException(name);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    public static void DrawBackground(Graphics g, int width, float camera, int stage)
    {
        Bitmap art = Map.Value;
        float offset = (float)(1 - Math.Cos(camera * .001f)) * 11;
        RectangleF scene = stage switch
        {
            4 or 5 => new RectangleF(410, 38, 275, 85),
            7 or 8 => new RectangleF(712, 20, 270, 80),
            _ => new RectangleF(72, 13, 215, 147)
        };
        var source = new RectangleF((scene.X + offset) * art.Width / 2048f,
            scene.Y * art.Height / 683f, scene.Width * art.Width / 2048f, scene.Height * art.Height / 683f);
        g.DrawImage(art, new RectangleF(0, 0, width, 440), source, GraphicsUnit.Pixel);
        var water = new RectangleF(414 * art.Width / 2048f, 150 * art.Height / 683f,
            69 * art.Width / 2048f, 23 * art.Height / 683f);
        for (int x = -((int)camera % 180); x < width; x += 180)
            g.DrawImage(art, new RectangleF(x, 440, 180, 80), water, GraphicsUnit.Pixel);
    }

    public static void DrawTerrain(Graphics g, Rectangle tile, float camera, int width)
    {
        Bitmap art = Map.Value;
        int left = Math.Max(tile.Left, (int)camera);
        int right = Math.Min(tile.Right, (int)camera + width + 1);
        var source = new RectangleF(91 * art.Width / 2048f, 198 * art.Height / 683f,
            135 * art.Width / 2048f, 22 * art.Height / 683f);
        for (int x = tile.Left + Math.Max(0, (left - tile.Left) / 160) * 160; x < right; x += 160)
        {
            int span = Math.Min(160, tile.Right - x);
            var sample = source with { Width = source.Width * span / 160f };
            g.DrawImage(art, new RectangleF(x, tile.Top, span, tile.Height), sample, GraphicsUnit.Pixel);
        }
    }

    public static void DrawHero(Graphics g, RectangleF body, int facing, int tick, bool walking, bool grounded, bool striking, int hurt, int heroId)
    {
        if (hurt > 0 && hurt / 4 % 2 == 0) return;
        Bitmap art = Hero.Value;
        var state = g.Save();
        float height = body.Height * 1.48f;
        ApprovedHeroArt.ApplyPose(g, body, facing, tick, walking, grounded, striking);
        g.TranslateTransform(0, -height);
        float scale = height / (heroId == 2 ? 690 : 668);
        g.ScaleTransform(scale, scale);
        g.TranslateTransform(heroId == 2 ? -531 : -183, heroId == 2 ? -65 : -91);
        using var outline = new GraphicsPath();
        outline.AddPolygon(heroId == 2 ? Pasha : Silhouette);
        g.SetClip(outline, CombineMode.Intersect);
        g.DrawImage(art, new Rectangle(0, 0, art.Width, art.Height));
        g.Restore(state);
    }
    public static void DrawGuardian(Graphics g, RectangleF body, bool exposed, int province)
    {
        Bitmap art = province <= 6 ? Map.Value : province <= 12 ? MiddleMap.Value : LastMap.Value;
        var state = g.Save();
        Point[] points = province switch
        {
            2 => [new(1771,167),new(1778,145),new(1783,128),new(1798,114),new(1814,106),
                new(1844,104),new(1875,116),new(1897,137),new(1906,155),new(1904,183),
                new(1894,191),new(1883,191),new(1874,174),new(1860,183),new(1847,192),
                new(1814,191),new(1803,182),new(1786,180)],
            3 => [new(728,313),new(752,309),new(788,319),new(816,337),new(824,317),
                new(843,308),new(855,324),new(882,309),new(938,300),new(943,313),
                new(931,336),new(910,353),new(898,380),new(885,398),new(849,409),
                new(804,409),new(766,395),new(753,370),new(738,343)],
            4 => [new(1751,405),new(1766,381),new(1789,374),new(1793,353),new(1807,337),
                new(1833,326),new(1860,328),new(1871,340),new(1881,322),new(1894,329),
                new(1898,351),new(1921,359),new(1932,381),new(1922,400),new(1902,402),
                new(1891,392),new(1876,406),new(1811,407),new(1791,397),new(1771,407)],
            5 => [new(749,625),new(761,605),new(775,589),new(780,575),new(797,563),
                new(827,554),new(850,554),new(878,561),new(893,574),new(903,596),
                new(891,619),new(871,628),new(815,630),new(787,620),new(771,625)],
            6 => [new(1743,620),new(1758,587),new(1783,568),new(1813,553),new(1841,549),
                new(1869,537),new(1873,519),new(1861,508),new(1842,518),new(1826,521),
                new(1825,506),new(1847,485),new(1864,482),new(1888,493),new(1903,513),
                new(1906,545),new(1899,565),new(1920,589),new(1928,620),new(1909,622),
                new(1893,606),new(1877,619),new(1851,624),new(1793,625),new(1776,617)],
            7 => [new(610,205),new(616,175),new(631,167),new(631,150),new(657,151),new(673,167),new(722,172),new(753,173),new(777,188),new(779,225),new(767,235),new(745,234),new(739,220),new(703,222),new(693,236),new(671,237),new(663,223),new(637,224)],
            8 => [new(1490,236),new(1500,202),new(1527,193),new(1545,179),new(1555,163),new(1575,157),new(1601,163),new(1615,178),new(1639,187),new(1667,198),new(1678,234),new(1658,239),new(1627,227),new(1599,237),new(1550,238),new(1519,230)],
            9 => [new(607,524),new(616,496),new(632,485),new(641,465),new(674,445),new(707,438),new(735,441),new(763,463),new(782,496),new(785,527),new(755,533),new(723,528),new(704,537),new(663,534),new(638,529)],
            10 => [new(1535,523),new(1543,482),new(1554,463),new(1582,461),new(1600,469),new(1628,473),new(1652,490),new(1668,518),new(1658,532),new(1614,532),new(1600,525),new(1580,532),new(1550,532)],
            11 => [new(605,813),new(616,783),new(635,767),new(656,752),new(671,728),new(710,715),new(744,701),new(771,707),new(766,737),new(745,764),new(757,798),new(773,829),new(752,839),new(720,825),new(687,832),new(651,832),new(620,824)],
            12 => [new(1440,800),new(1449,775),new(1475,756),new(1507,752),new(1538,753),new(1569,763),new(1605,765),new(1646,777),new(1682,796),new(1702,817),new(1676,831),new(1637,828),new(1605,816),new(1557,822),new(1526,827),new(1489,819),new(1453,816)],
            13 => [new(561,235),new(578,203),new(593,177),new(615,159),new(636,143),new(662,130),new(682,145),new(717,137),new(753,150),new(781,179),new(798,211),new(800,241),new(752,241),new(707,226),new(681,242),new(653,240),new(623,235),new(596,243)],
            14 => [new(1450,216),new(1453,192),new(1468,177),new(1501,177),new(1522,158),new(1547,147),new(1585,145),new(1617,159),new(1640,185),new(1657,213),new(1656,241),new(1615,240),new(1598,229),new(1555,240),new(1530,234),new(1507,238),new(1482,222)],
            15 => [new(548,515),new(570,483),new(603,475),new(628,454),new(646,443),new(670,442),new(693,427),new(727,416),new(760,420),new(786,437),new(792,466),new(781,490),new(813,502),new(815,533),new(779,532),new(757,521),new(724,531),new(690,532),new(659,521),new(624,531),new(579,531)],
            16 => [new(1470,360),new(1501,363),new(1541,387),new(1564,414),new(1583,394),new(1612,349),new(1635,325),new(1665,321),new(1683,342),new(1690,373),new(1674,420),new(1650,451),new(1634,483),new(1643,515),new(1627,523),new(1575,521),new(1548,508),new(1527,491),new(1504,454),new(1483,410)],
            17 => [new(590,761),new(599,723),new(615,700),new(650,684),new(678,679),new(712,689),new(746,700),new(773,723),new(789,756),new(785,787),new(782,806),new(748,813),new(730,798),new(709,803),new(673,808),new(639,806),new(619,788),new(604,782)],
            18 => [new(1480,782),new(1489,735),new(1517,707),new(1536,688),new(1574,681),new(1604,658),new(1632,656),new(1661,669),new(1682,691),new(1705,712),new(1722,737),new(1729,777),new(1727,802),new(1697,802),new(1669,791),new(1638,803),new(1602,804),new(1574,793),new(1543,803),new(1509,800)],
            _ => [new(768,172), new(778,149), new(789,132), new(802,126),
                new(801,111), new(809,102), new(827,99), new(838,105), new(847,119),
                new(856,119), new(870,127), new(879,139), new(886,157), new(886,176),
                new(878,185), new(858,189), new(844,184), new(829,185), new(813,189),
                new(799,182), new(781,187), new(769,181)]
        };
        int left = points.Min(p => p.X), top = points.Min(p => p.Y);
        int width = points.Max(p => p.X) - left, height = points.Max(p => p.Y) - top;
        float scale = Math.Min(body.Height / height, body.Width * 1.6f / width);
        g.TranslateTransform(body.X + (body.Width - width * scale) / 2, body.Bottom - height * scale);
        g.ScaleTransform(scale, scale);
        g.TranslateTransform(-left, -top);
        using var outline = new GraphicsPath();
        outline.AddPolygon(points);
        g.SetClip(outline, CombineMode.Intersect);
        g.DrawImage(art, new Rectangle(0, 0, province <= 6 ? 2048 : 1774, province <= 6 ? 683 : 887));
        if (exposed)
        {
            using var tint = new SolidBrush(Color.FromArgb(50, Color.Gold));
            g.FillPath(tint, outline);
        }
        g.Restore(state);
        if (exposed)
            g.DrawEllipse(Pens.Gold, body);
    }
}
