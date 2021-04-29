using CddaOsmMaps.Cdda;
using System.Collections.Generic;
using Xunit;

namespace CddaOsmMaps_XUnit
{
    public class CddaTests
    {
        [Theory]
        [MemberData(nameof(TestCddaPlayerCoordData))]
        public void TestCddaPlayerCoord((int x, int y) abspos, CddaPlayerCoords expected)
        {
            var result = new CddaPlayerCoords(abspos);

            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> TestCddaPlayerCoordData =>
            new List<object[]>
            {
                ExpectedCddaPlayerCoord(
                    // player on abspos (0,0)
                    abspos: (0, 0),
                    overmapRegion: (-1, -1),
                    savegameLev: (355, 355),
                    savegamePos: (60, 60)
                ),
                ExpectedCddaPlayerCoord(
                    // reality bubble top left on abspos (0,0)
                    abspos: (60, 60),
                    overmapRegion: (0, 0),
                    savegameLev: (0, 0),
                    savegamePos: (60, 60)
                ),
                ExpectedCddaPlayerCoord(
                    // reality bubble top left on abspos (-1,-1)
                    abspos: (59, 59),
                    overmapRegion: (-1, -1),
                    savegameLev: (359, 359),
                    savegamePos: (71, 71)
                ),
                ExpectedCddaPlayerCoord(
                    // player on (1,1) of region (1,1)
                    abspos: (4320, 4320),
                    overmapRegion: (0, 0),
                    savegameLev: (355, 355),
                    savegamePos: (60, 60)
                ),
                ExpectedCddaPlayerCoord(
                    // reality bubble top left on (1,1) of region (1,1)
                    abspos: (4380, 4380),
                    overmapRegion: (1, 1),
                    savegameLev: (0, 0),
                    savegamePos: (60, 60)
                ),
                ExpectedCddaPlayerCoord(
                    // player on abspos (0,4319)
                    abspos: (0, 4319),
                    overmapRegion: (-1, 0),
                    savegameLev: (355, 354),
                    savegamePos: (60, 71)
                ),
            };

        private static object[] ExpectedCddaPlayerCoord(
            (int, int) abspos,
            (int, int) overmapRegion,
            (int, int) savegameLev,
            (int, int) savegamePos
        ) => new object[] {
            abspos,
            new CddaPlayerCoords(
                abspos,
                overmapRegion,
                savegameLev,
                savegamePos
            )
        };
    }
}
