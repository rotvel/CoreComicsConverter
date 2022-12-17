﻿namespace PdfConverter.Helpers
{
    public sealed class StatsCount
    {
        private static volatile int largestPng = 0;
        private static long totalPngSize = 0;

        private static volatile int largestJpg = 0;
        private static long totalJpgSize = 0;

        private static volatile int largestPipeRead = 0;
        private static volatile int pipeReadCount = 0;

        public static void AddPipeRead(int read)
        {
            pipeReadCount++;

            if (read > largestPipeRead)
            {
                largestPipeRead = read;
            }
        }

        public static volatile int AllBuffers = 0;
        public static volatile int ExpandedBuffers = 0;

        private static volatile int totalConversionTime = 0;
        private static volatile int imageConversionCount = 0;
        private static volatile int imageResizeCount = 0;
        private static volatile int jpgLargerThanPngCount = 0;
        private static volatile int largerBuffersNeededCount = 0;

        public static void AddImageConversion(int ms, bool resize, int png, int jpg, bool largerBufferNeeded)
        {
            imageConversionCount++;

            if (resize)
            {
                imageResizeCount++;
            }

            totalConversionTime += ms;

            totalPngSize += png;

            if (png > largestPng)
            {
                largestPng = png;
            }

            totalJpgSize += jpg;

            if (jpg > largestJpg)
            { 
                largestJpg = jpg;
            }

            if (jpg > png)
            {
                jpgLargerThanPngCount++;
            }

            if (largerBufferNeeded)
            {
                largerBuffersNeededCount++;
            }
        }

        public static void ShowStats()
        {
            if (pipeReadCount > 0)
            {
                Console.WriteLine($"Pipe reads: {pipeReadCount} Largest read: {largestPipeRead}");
            }

            if (imageConversionCount > 0)
            {
                Console.WriteLine($"Image conversions: {imageConversionCount} (resizes: {imageResizeCount}) Average ms: {totalConversionTime / imageConversionCount}");
                Console.WriteLine($"Largest Png: {largestPng} Average: {totalPngSize / imageConversionCount}");
                Console.WriteLine($"Largest Jpg: {largestJpg} Average: {totalJpgSize / imageConversionCount}");
                Console.WriteLine($"Jpg > Png: {jpgLargerThanPngCount} Emergency buffers needed: {largerBuffersNeededCount}"); ;
            }

            if (AllBuffers > 0)
            {
                Console.WriteLine($"All buffers: {AllBuffers} (expansions: {ExpandedBuffers})");
            }
        }
    }
}
