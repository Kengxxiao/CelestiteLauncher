using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    internal class PerformanceUnitTests
    {
        [Test]
        public void ToKana_WhenConvertingRomajiToHiragana_MeanSpeedLessThanTenMs()
        {
            var input = "aiueosashisusesonaninunenokakikukeko";
            var iterations = 10000;

            List<long> runs = new();

            for (var i = 0; i < iterations; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                WanaKana.ToKana(input);
                watch.Stop();

                runs.Add(watch.ElapsedMilliseconds);
            }

            var meanSpeed = runs.Average();

            Assert.Less(meanSpeed, 10);
        }

        [Test]
        public void ToKana_ConvertingRomajiToKatakana_MeanSpeedLessThanTenMs()
        {
            var input = "AIUEOSASHISUSESONANINUNENOKAKIKUKEKO";
            var iterations = 10000;

            List<long> runs = new();

            for (var i = 0; i < iterations; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                WanaKana.ToKana(input);
                watch.Stop();

                runs.Add(watch.ElapsedMilliseconds);
            }

            var meanSpeed = runs.Average();

            Assert.Less(meanSpeed, 10);
        }

        [Test]
        public void ToHiragana_ConvertingFromRomaji_MeanSpeedLessThanTenMs()
        {
            var input = "aiueosashisusesonaninunenokakikukeko";
            var iterations = 10000;

            List<long> runs = new();

            for (var i = 0; i < iterations; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                WanaKana.ToHiragana(input);
                watch.Stop();

                runs.Add(watch.ElapsedMilliseconds);
            }

            var meanSpeed = runs.Average();

            Assert.Less(meanSpeed, 10);
        }

        [Test]
        public void ToHiragana_ConvertingFromKatakana_MeanSpeedLessThanTenMs()
        {
            var input = "アイウエオサシスセソナニヌネノカキクケコ";
            var iterations = 10000;

            List<long> runs = new();

            for (var i = 0; i < iterations; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                WanaKana.ToHiragana(input);
                watch.Stop();

                runs.Add(watch.ElapsedMilliseconds);
            }

            var meanSpeed = runs.Average();

            Assert.Less(meanSpeed, 10);
        }


        [Test]
        public void ToKatakana_ConvertingFromRomaji_MeanSpeedLessThanTenMs()
        {
            var input = "aiueosashisusesonaninunenokakikukeko";
            var iterations = 10000;

            List<long> runs = new();

            for (var i = 0; i < iterations; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                WanaKana.ToKatakana(input);
                watch.Stop();

                runs.Add(watch.ElapsedMilliseconds);
            }

            var meanSpeed = runs.Average();

            Assert.Less(meanSpeed, 10);
        }

        [Test]
        public void ToKatakana_ConvertingFromHiragana_MeanSpeedLessThanTenMs()
        {
            var input = "あいうえおさしすせそなにぬねのかきくけこ";
            var iterations = 10000;

            List<long> runs = new();

            for (var i = 0; i < iterations; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                WanaKana.ToKatakana(input);
                watch.Stop();

                runs.Add(watch.ElapsedMilliseconds);
            }

            var meanSpeed = runs.Average();

            Assert.Less(meanSpeed, 10);
        }

        [Test]
        public void ToRomaji_ConvertingFromHiragana_MeanSpeedLessThanTenMs()
        {
            var input = "あいうえおさしすせそなにぬねのかきくけこ";
            var iterations = 10000;

            List<long> runs = new();

            for (var i = 0; i < iterations; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                WanaKana.ToRomaji(input);
                watch.Stop();

                runs.Add(watch.ElapsedMilliseconds);
            }

            var meanSpeed = runs.Average();

            Assert.Less(meanSpeed, 10);
        }

        [Test]
        public void ToRomaji_ConvertingFromKatakana_MeanSpeedLessThanTenMs()
        {
            var input = "アイウエオサシスセソナニヌネノカキクケコ";
            var iterations = 10000;

            List<long> runs = new();

            for (var i = 0; i < iterations; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                WanaKana.ToRomaji(input);
                watch.Stop();

                runs.Add(watch.ElapsedMilliseconds);
            }

            var meanSpeed = runs.Average();

            Assert.Less(meanSpeed, 10);
        }
    }
}