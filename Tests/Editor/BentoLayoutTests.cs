using System.Collections.Generic;
using NUnit.Framework;

namespace UIScroller.Tests.Editor
{
    /// <summary>The bento layout model: span sequences packed in order, patterns, and the JSON format with its validation.</summary>
    public class BentoLayoutTests
    {
        private static readonly BentoPattern Hero = new(2, new[]
        {
            new BentoTile(0, 0, 2, 2),
            new BentoTile(2, 0, 2, 1),
            new BentoTile(2, 1, 1, 1),
            new BentoTile(3, 1, 1, 1)
        });

        private static readonly BentoPattern Strip = new(1, new[]
        {
            new BentoTile(0, 0, 1, 1),
            new BentoTile(1, 0, 1, 1),
            new BentoTile(2, 0, 1, 1),
            new BentoTile(3, 0, 1, 1)
        });

        private static List<BentoTile> Arrange(BentoLayout layout, int count)
        {
            BentoArranger arranger = layout.CreateArranger();
            List<BentoTile> tiles = new(count);
            for (int i = 0; i < count; i++)
                tiles.Add(arranger.Next());
            return tiles;
        }

        private static void AssertTiles(IList<BentoTile> actual, params BentoTile[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], actual[i], $"item {i}");
        }

        // ---------- Spans, packed in order ----------

        [Test]
        public void Uniform_FillsRowsLeftToRight()
        {
            List<BentoTile> tiles = Arrange(BentoLayout.Uniform(4), 6);

            AssertTiles(tiles,
                new BentoTile(0, 0, 1, 1), new BentoTile(1, 0, 1, 1), new BentoTile(2, 0, 1, 1), new BentoTile(3, 0, 1, 1),
                new BentoTile(0, 1, 1, 1), new BentoTile(1, 1, 1, 1));
        }

        [Test]
        public void Spans_ABigFirstTile_AndSmallOnesAroundIt()
        {
            BentoLayout layout = BentoLayout.FromSpans(4, new[] { new BentoSpan(2, 2), BentoSpan.One }, repeatFrom: 1);

            AssertTiles(Arrange(layout, 6),
                new BentoTile(0, 0, 2, 2),
                new BentoTile(2, 0, 1, 1), new BentoTile(3, 0, 1, 1),
                new BentoTile(2, 1, 1, 1), new BentoTile(3, 1, 1, 1),
                new BentoTile(0, 2, 1, 1));
        }

        [Test]
        public void Spans_ATileThatDoesNotFit_MovesToTheNextRow_LeavingAHole()
        {
            BentoLayout layout = BentoLayout.FromSpans(3, new[] { BentoSpan.One, BentoSpan.One, new BentoSpan(2, 1), BentoSpan.One }, repeatFrom: 3);

            AssertTiles(Arrange(layout, 4),
                new BentoTile(0, 0, 1, 1), new BentoTile(1, 0, 1, 1),
                new BentoTile(0, 1, 2, 1),
                new BentoTile(2, 1, 1, 1));
        }

        [Test]
        public void Spans_NeverGoBack_ToFillAnEarlierHole()
        {
            BentoLayout layout = BentoLayout.FromSpans(3, new[] { new BentoSpan(2, 1), new BentoSpan(2, 1), BentoSpan.One }, repeatFrom: 2);

            AssertTiles(Arrange(layout, 3),
                new BentoTile(0, 0, 2, 1),
                new BentoTile(0, 1, 2, 1),
                new BentoTile(2, 1, 1, 1));
        }

        [Test]
        public void Spans_ATallTile_KeepsItsCellsBelow()
        {
            BentoLayout layout = BentoLayout.FromSpans(2, new[] { new BentoSpan(1, 2), BentoSpan.One }, repeatFrom: 1);

            AssertTiles(Arrange(layout, 4),
                new BentoTile(0, 0, 1, 2),
                new BentoTile(1, 0, 1, 1),
                new BentoTile(1, 1, 1, 1),
                new BentoTile(0, 2, 1, 1));
        }

        [Test]
        public void Spans_RepeatFromZero_CyclesTheWholeSequence()
        {
            BentoLayout layout = BentoLayout.FromSpans(2, new[] { new BentoSpan(2, 1), BentoSpan.One });

            AssertTiles(Arrange(layout, 6),
                new BentoTile(0, 0, 2, 1), new BentoTile(0, 1, 1, 1),
                new BentoTile(0, 2, 2, 1), new BentoTile(0, 3, 1, 1),
                new BentoTile(0, 4, 2, 1), new BentoTile(0, 5, 1, 1));
        }

        [Test]
        public void Spans_ManyRandomTiles_StayInOrder_InBounds_AndNeverOverlap()
        {
            const int columns = 5;
            const int count = 600;
            System.Random random = new(1234);
            List<BentoSpan> spans = new();
            for (int i = 0; i < 40; i++)
                spans.Add(new BentoSpan(random.Next(1, 4), random.Next(1, 4)));

            List<BentoTile> tiles = Arrange(BentoLayout.FromSpans(columns, spans), count);

            HashSet<(int, int)> used = new();
            for (int i = 0; i < count; i++)
            {
                BentoTile tile = tiles[i];
                Assert.GreaterOrEqual(tile.Column, 0);
                Assert.LessOrEqual(tile.Column + tile.Columns, columns, $"item {i} out of bounds");
                if (i > 0)
                {
                    BentoTile previous = tiles[i - 1];
                    bool inOrder = tile.Row > previous.Row || (tile.Row == previous.Row && tile.Column > previous.Column);
                    Assert.IsTrue(inOrder, $"item {i} {tile} starts before item {i - 1} {previous}");
                }

                for (int r = tile.Row; r < tile.Row + tile.Rows; r++)
                for (int c = tile.Column; c < tile.Column + tile.Columns; c++)
                    Assert.IsTrue(used.Add((c, r)), $"item {i} overlaps at {c},{r}");
            }
        }

        [Test]
        public void Arranger_Reset_StartsAgainFromTheTop()
        {
            BentoArranger arranger = BentoLayout.FromSpans(4, new[] { new BentoSpan(2, 2), BentoSpan.One }, 1).CreateArranger();
            List<BentoTile> first = new();
            for (int i = 0; i < 30; i++)
                first.Add(arranger.Next());

            arranger.Reset();
            for (int i = 0; i < 30; i++)
                Assert.AreEqual(first[i], arranger.Next(), $"item {i}");
        }

        [Test]
        public void Spans_WiderThanTheColumns_AreAnError()
        {
            BentoLayout layout = BentoLayout.FromSpans(3, new[] { new BentoSpan(4, 1) });

            Assert.IsFalse(layout.IsValid);
            StringAssert.Contains("spans[0]", layout.Errors[0]);
        }

        [Test]
        public void MaxRowSpan_IsTheTallestTile()
        {
            Assert.AreEqual(1, BentoLayout.Uniform(3).MaxRowSpan);
            Assert.AreEqual(3, BentoLayout.FromSpans(4, new[] { BentoSpan.One, new BentoSpan(1, 3), new BentoSpan(2, 2) }).MaxRowSpan);
            Assert.AreEqual(2, BentoLayout.FromPatterns(4, new[] { Hero, Strip }).MaxRowSpan);
        }

        // ---------- Patterns ----------

        [Test]
        public void Pattern_Repeats_BelowItself()
        {
            List<BentoTile> tiles = Arrange(BentoLayout.FromPatterns(4, new[] { Hero }), 6);

            AssertTiles(tiles,
                new BentoTile(0, 0, 2, 2), new BentoTile(2, 0, 2, 1), new BentoTile(2, 1, 1, 1), new BentoTile(3, 1, 1, 1),
                new BentoTile(0, 2, 2, 2), new BentoTile(2, 2, 2, 1));
        }

        [Test]
        public void Patterns_RepeatFrom_SkipsTheIntroOnLaterRounds()
        {
            List<BentoTile> tiles = Arrange(BentoLayout.FromPatterns(4, new[] { Hero, Strip }, repeatFrom: 1), 12);

            Assert.AreEqual(new BentoTile(0, 0, 2, 2), tiles[0]);
            Assert.AreEqual(new BentoTile(0, 2, 1, 1), tiles[4]);
            Assert.AreEqual(new BentoTile(3, 2, 1, 1), tiles[7]);
            Assert.AreEqual(new BentoTile(0, 3, 1, 1), tiles[8]);
            Assert.AreEqual(new BentoTile(3, 3, 1, 1), tiles[11]);
        }

        [Test]
        public void Pattern_TilesListedOutOfOrder_AreFilledInReadingOrder()
        {
            BentoPattern shuffled = new(2, new[]
            {
                new BentoTile(3, 1, 1, 1),
                new BentoTile(2, 0, 2, 1),
                new BentoTile(0, 0, 2, 2),
                new BentoTile(2, 1, 1, 1)
            });

            AssertTiles(Arrange(BentoLayout.FromPatterns(4, new[] { shuffled }), 4),
                new BentoTile(0, 0, 2, 2), new BentoTile(2, 0, 2, 1), new BentoTile(2, 1, 1, 1), new BentoTile(3, 1, 1, 1));
        }

        [Test]
        public void Pattern_WithOverlappingTiles_IsAnError()
        {
            BentoPattern overlapping = new(2, new[] { new BentoTile(0, 0, 2, 2), new BentoTile(1, 1, 1, 1) });

            BentoLayout layout = BentoLayout.FromPatterns(4, new[] { overlapping });

            Assert.IsFalse(layout.IsValid);
            StringAssert.Contains("overlap", layout.Errors[0]);
        }

        [Test]
        public void Pattern_WithATileOutside_IsAnError()
        {
            Assert.IsFalse(BentoLayout.FromPatterns(4, new[] { new BentoPattern(1, new[] { new BentoTile(3, 0, 2, 1) }) }).IsValid);
            Assert.IsFalse(BentoLayout.FromPatterns(4, new[] { new BentoPattern(1, new[] { new BentoTile(0, 0, 1, 2) }) }).IsValid);
        }

        [Test]
        public void Pattern_WithNoTilesOrNoRows_IsAnError()
        {
            Assert.IsFalse(BentoLayout.FromPatterns(4, new[] { new BentoPattern(1, new BentoTile[0]) }).IsValid);
            Assert.IsFalse(BentoLayout.FromPatterns(4, new[] { new BentoPattern(0, new[] { new BentoTile(0, 0, 1, 1) }) }).IsValid);
        }

        [Test]
        public void AnInvalidLayout_FallsBackToOneByOneTiles()
        {
            BentoLayout layout = BentoLayout.FromPatterns(4, new[] { new BentoPattern(1, new BentoTile[0]) });

            AssertTiles(Arrange(layout, 5),
                new BentoTile(0, 0, 1, 1), new BentoTile(1, 0, 1, 1), new BentoTile(2, 0, 1, 1), new BentoTile(3, 0, 1, 1),
                new BentoTile(0, 1, 1, 1));
        }

        // ---------- JSON ----------

        [Test]
        public void Json_Spans()
        {
            BentoLayout layout = BentoLayout.FromJson(@"{ ""columns"": 4, ""repeatFrom"": 1, ""spans"": [ {""w"":2,""h"":2}, {""w"":1,""h"":1} ] }");

            Assert.IsTrue(layout.IsValid, string.Join("; ", layout.Errors));
            Assert.AreEqual(4, layout.Columns);
            AssertTiles(Arrange(layout, 3), new BentoTile(0, 0, 2, 2), new BentoTile(2, 0, 1, 1), new BentoTile(3, 0, 1, 1));
        }

        [Test]
        public void Json_Patterns()
        {
            const string json = @"{
                ""columns"": 4,
                ""repeatFrom"": 1,
                ""patterns"": [
                    { ""rows"": 2, ""tiles"": [ {""c"":0,""r"":0,""w"":2,""h"":2}, {""c"":2,""r"":0,""w"":2,""h"":1}, {""c"":2,""r"":1,""w"":1,""h"":1}, {""c"":3,""r"":1,""w"":1,""h"":1} ] },
                    { ""rows"": 1, ""tiles"": [ {""c"":0,""r"":0,""w"":1,""h"":1}, {""c"":1,""r"":0,""w"":1,""h"":1}, {""c"":2,""r"":0,""w"":1,""h"":1}, {""c"":3,""r"":0,""w"":1,""h"":1} ] }
                ]
            }";

            BentoLayout layout = BentoLayout.FromJson(json);

            Assert.IsTrue(layout.IsValid, string.Join("; ", layout.Errors));
            List<BentoTile> expected = Arrange(BentoLayout.FromPatterns(4, new[] { Hero, Strip }, 1), 16);
            AssertTiles(Arrange(layout, 16), expected.ToArray());
        }

        [Test]
        public void Json_WithOnlyColumns_IsAUniformGrid()
        {
            BentoLayout layout = BentoLayout.FromJson(@"{ ""columns"": 3 }");

            Assert.IsTrue(layout.IsValid);
            AssertTiles(Arrange(layout, 4), new BentoTile(0, 0, 1, 1), new BentoTile(1, 0, 1, 1), new BentoTile(2, 0, 1, 1), new BentoTile(0, 1, 1, 1));
        }

        [Test]
        public void Json_Malformed_IsAnError_AndFallsBack()
        {
            BentoLayout layout = BentoLayout.FromJson("{ columns: 4, ");

            Assert.IsFalse(layout.IsValid);
            StringAssert.Contains("JSON", layout.Errors[0]);
            Assert.AreEqual(0, layout.Columns, "unknown columns: the list uses its own count");
            BentoArranger arranger = layout.CreateArranger(fallbackColumns: 4);
            arranger.Next();
            Assert.AreEqual(new BentoTile(1, 0, 1, 1), arranger.Next());
        }

        [Test]
        public void Json_Empty_IsAnError()
        {
            Assert.IsFalse(BentoLayout.FromJson("").IsValid);
            Assert.IsFalse(BentoLayout.FromJson(null).IsValid);
        }

        [Test]
        public void Json_WithBothSpansAndPatterns_IsAnError()
        {
            BentoLayout layout = BentoLayout.FromJson(@"{ ""columns"": 2, ""spans"": [ {""w"":1,""h"":1} ], ""patterns"": [ { ""rows"": 1, ""tiles"": [ {""c"":0,""r"":0,""w"":1,""h"":1} ] } ] }");

            Assert.IsFalse(layout.IsValid);
            StringAssert.Contains("spans", layout.Errors[0]);
            StringAssert.Contains("patterns", layout.Errors[0]);
        }

        [TestCase(0)]
        [TestCase(65)]
        [TestCase(-2)]
        public void Json_ColumnsOutOfRange_IsAnError(int columns)
        {
            BentoLayout layout = BentoLayout.FromJson($@"{{ ""columns"": {columns} }}");

            Assert.IsFalse(layout.IsValid);
            StringAssert.Contains("columns", layout.Errors[0]);
            Assert.AreEqual(0, layout.Columns);
        }

        [Test]
        public void Json_RepeatFromOutOfRange_IsAnError()
            => Assert.IsFalse(BentoLayout.FromJson(@"{ ""columns"": 2, ""repeatFrom"": 2, ""spans"": [ {""w"":1,""h"":1}, {""w"":1,""h"":1} ] }").IsValid);

        [Test]
        public void Json_ASpanWithoutSize_IsAnError()
        {
            BentoLayout layout = BentoLayout.FromJson(@"{ ""columns"": 2, ""spans"": [ {""w"":1,""h"":1}, {""w"":1} ] }");

            Assert.IsFalse(layout.IsValid);
            StringAssert.Contains("spans[1]", layout.Errors[0]);
        }

        [Test]
        public void Json_ErrorsNameThePattern()
        {
            BentoLayout layout = BentoLayout.FromJson(@"{ ""columns"": 2, ""patterns"": [ { ""rows"": 1, ""tiles"": [ {""c"":0,""r"":0,""w"":1,""h"":1} ] }, { ""rows"": 1, ""tiles"": [ {""c"":1,""r"":0,""w"":2,""h"":1} ] } ] }");

            Assert.IsFalse(layout.IsValid);
            StringAssert.Contains("patterns[1]", layout.Errors[0]);
        }
    }
}
