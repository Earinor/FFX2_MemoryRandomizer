﻿using FFX2MemoryReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MemoryRandomizer.Core
{
    public class GameManager
    {
        const uint startofDresssphereSaves = 0xA00D1C; // 0x9FBD60; // in process (this - 0x4fbc = ProcessStart)
        const uint startOfGGSaves = 0xA00D14;
        const uint ptrOffset = 0x4fbc; // Process + 0x4fbc = Dressspheres
        const uint offset = 0;
        private static ProcessMemoryReader mReader;
        private static BinaryReader br;

        static string mode;

        private static bool initialReadDoneDS = false;
        private static bool initialReadDoneGG = false;
        private static bool initialReadDoneBoth = false;
        private static byte[] newByteArrayDS = new byte[0x1E];
        private static byte[] newByteArrayGG = new byte[0x8];
        private static int bytesOutDS;
        private static int bytesOutGG;
        private static int bytesIn;

        private static Process mGameProcess;
        private static Serializer mSerializer = new Serializer();
        private static DresssphereMapping dm;
        private static GarmentGridMapping ggm;
        private static RandomizableItemMapping rim;
        private static ByteArrayHandler byteArrayHandler;

        /// <summary>
        ///     Starts the randomization process.
        /// </summary>
        /// <param name="randomizeDS"></param>
        /// <param name="randomizeGG"></param>
        /// <param name="randomizeBoth"></param>
        public void Startup(bool randomizeDS, bool randomizeGG, bool randomizeBoth, bool loadDS = true, bool loadGG = true, bool loadBoth = true)
        {
            // Read save files
            if (randomizeBoth && loadBoth)
            {
                initialReadDoneBoth = SaveManager.ReadSaveFile(rim, SaveManager.GGSaveFileName);
            }
            if (randomizeDS && loadDS)
            {
                initialReadDoneDS = SaveManager.ReadSaveFile(dm, SaveManager.DresssphereSaveFileName);
            }
            if (randomizeGG && loadGG)
            {
                initialReadDoneGG = SaveManager.ReadSaveFile(ggm, SaveManager.GGSaveFileName);
            }

            // Attach to process
            FindAndOpenGameProcess();

            // Initial read
            DoInitialReadsAndShuffle(randomizeBoth);
            byteArrayHandler = new ByteArrayHandler(dm, ggm, rim);

            // Start monitoring
            try
            {
                Monitor(randomizeDS, randomizeGG, randomizeBoth);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                Console.WriteLine("Closing application...");
                throw exc;
            }
        }

        private void Monitor(bool randomizeDS, bool randomizeGG, bool randomizeBoth)
        {
            while (true)
            {
                bytesOutDS = 0;
                bytesOutGG = 0;
                byte[] memoryBytesDs = mReader.ReadMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startofDresssphereSaves), 0x1E, out bytesOutDS);
                byte[] memoryBytesGG = mReader.ReadMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startOfGGSaves), 0x8, out bytesOutGG);
                if (bytesOutDS <= 0 || bytesOutGG <= 0)
                {
                    FindAndOpenGameProcess();
                }
                else
                {
                    if (randomizeBoth)
                    {
                        // check byteArray for changes -> apply changes to mapping 
                        byteArrayHandler.CheckReadBytesBoth(ref memoryBytesDs, ref memoryBytesGG);
                        // Write mapping data to memory
                        newByteArrayGG = new byte[0x8];
                        byteArrayHandler.CreateByteArrayBoth(ref newByteArrayDS, ref newByteArrayGG);

                        int error = mReader.WriteMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startofDresssphereSaves), newByteArrayDS, out bytesIn);
                        int error2 = mReader.WriteMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startOfGGSaves), newByteArrayGG, out bytesIn);
                        this.CheckError(error, error2);

                        mSerializer.SaveMapping(SaveManager.BothSaveFileName, rim.MappingList);
                    }
                    if (randomizeDS && !randomizeBoth)
                    {
                        // check byteArray for changes -> apply changes to mapping 
                        byteArrayHandler.CheckReadBytesDS(ref memoryBytesDs);
                        // Write mapping data to memory
                        byteArrayHandler.CreateByteArrayDS(ref newByteArrayDS);

                        int error = mReader.WriteMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startofDresssphereSaves), newByteArrayDS, out bytesIn);
                        this.CheckError(error);

                        mSerializer.SaveMapping(SaveManager.DresssphereSaveFileName, dm.MappingList);
                    }

                    if (randomizeGG && !randomizeBoth)
                    {
                        byteArrayHandler.CheckReadBytesGG(ref memoryBytesGG);
                        newByteArrayGG = new byte[0x8];
                        byteArrayHandler.CreateByteArrayGG(ref newByteArrayGG);

                        int error = mReader.WriteMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startOfGGSaves), newByteArrayGG, out bytesIn);
                        this.CheckError(error);

                        mSerializer.SaveMapping(SaveManager.GGSaveFileName, ggm.MappingList);
                    }
                    Thread.Sleep(500);
                }
            }
        }

        private void CheckError(params int[] errors)
        {
            foreach (int error in errors)
            {
                if (error != 0)
                {
                    throw new IOException($"Write Memory returned with error code {error}");
                }
            }
        }

        #region process
        private static Process FindGameProcess()
        {
            Process[] processes;
            do
            {
                processes = Process.GetProcessesByName("FFX-2");
                if (processes.Length > 0)
                {
                    return processes[0];
                }
                else
                {
                    Console.WriteLine("Process not found, please start the game.");
                    Thread.Sleep(500);
                }
            } while (processes.Length < 1);
            return null;
        }
        private static void FindAndOpenGameProcess()
        {
            mGameProcess = FindGameProcess();
            if (mGameProcess != null)
            {
                mReader = new ProcessMemoryReader(mGameProcess);
                mReader.OpenProcess();
            }
        }
        #endregion process


        private static void DoInitialReadsAndShuffle(bool randomizeBoth)
        {
            while (randomizeBoth && !initialReadDoneBoth)
            {
                byte[] initialMemoryBytesDS = mReader.ReadMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startofDresssphereSaves), 0x1E, out bytesOutDS);
                byte[] initialMemoryBytesGG = mReader.ReadMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startOfGGSaves), 0x8, out bytesOutGG);

                rim = new RandomizableItemMapping(initialMemoryBytesDS, initialMemoryBytesGG);
                initialReadDoneBoth = DoInitialShuffle(rim, bytesOutDS <= 0 || bytesOutGG <= 0);
            }
            while (!initialReadDoneDS)
            {
                byte[] initialMemoryBytes = mReader.ReadMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startofDresssphereSaves), 0x1E, out bytesOutDS);

                dm = new DresssphereMapping(initialMemoryBytes);
                initialReadDoneDS = DoInitialShuffle(dm, bytesOutDS <= 0);
            }
            while (!initialReadDoneGG)
            {
                byte[] initialMemoryBytes = mReader.ReadMemory((IntPtr)((uint)mGameProcess.Modules[0].BaseAddress + startOfGGSaves), 0x8, out bytesOutGG);

                ggm = new GarmentGridMapping(initialMemoryBytes);
                initialReadDoneGG = DoInitialShuffle(ggm, bytesOutGG <= 0);
            }
        }

        private static bool DoInitialShuffle<T>(IMapping<T> mapping, bool readSuccessful)
        {
            if (readSuccessful)
            {
                FindAndOpenGameProcess();

                return false;
            }
            else
            {
                mapping.Initiate();
                // Initiate randomization
                Randomizer.Shuffle(mapping.RandomizableItems);
                mapping.CreateMapping();

                return true;
            }
        }
    }
}