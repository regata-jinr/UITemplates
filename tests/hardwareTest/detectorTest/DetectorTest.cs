﻿/***************************************************************************
 *                                                                         *
 *                                                                         *
 * Copyright(c) 2017-2021, REGATA Experiment at FLNP|JINR                  *
 * Author: [Boris Rumyantsev](mailto:bdrum@jinr.ru)                        *
 *                                                                         *
 * The REGATA Experiment team license this file to you under the           *
 * GNU GENERAL PUBLIC LICENSE                                              *
 *                                                                         *
 ***************************************************************************/

using CanberraDataAccessLib;
using System;
using System.IO;
using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Regata.Core.Hardware;
using Regata.Core.DataBase.Models;
using Regata.Core.Settings;

namespace Regata.Tests.Hardware.Detectors
{
    //TODO: add stress-test!
 
    [TestClass]
    public class DetectorsTest
    {
        public Detector _d1;
        DataAccess f1;

        public DetectorsTest()
        {
            _d1 = new Detector("D1", "bdrum");
            f1 = new DataAccess();
        }

        [TestMethod]
        public void Names()
        {
            Assert.AreEqual("D1", _d1.Name);
        }

        [TestMethod]
        public void Statuses()
        {
            Assert.AreEqual(DetectorStatus.ready, _d1.Status);
        }

        [TestMethod]
        public void Connections()
        {
            Assert.IsTrue(_d1.IsConnected);
        }

        [TestMethod]
        public void Disconnections()
        {
            System.Threading.Thread.Sleep(2000);

            Assert.IsTrue(_d1.IsConnected);
            _d1.Disconnect();

            System.Threading.Thread.Sleep(2000);

            Assert.IsFalse(_d1.IsConnected);
            Assert.AreEqual(DetectorStatus.off, _d1.Status);

            _d1.Connect();
        }

        [TestMethod]
        public void StartStopStartStopSave()
        {
            var sd = new Irradiation()
            {
                CountryCode = "RO",
                ClientNumber = "2",
                Year = "19",
                SetNumber = "12",
                SetIndex = "b",
                SampleNumber = "2",
                Assistant = 1,
                Note = "test2",
                DateTimeStart = DateTime.Now,
                DateTimeFinish = DateTime.Now.AddSeconds(3),
                Duration = 3
            };

            var configuration = new MapperConfiguration(cfg => cfg.AddMaps("base"));
            var mapper = new Mapper(configuration);
            var m = mapper.Map<Measurement>(sd);
            m.Duration = 5;
            m.Detector = "D1";
            m.Height = 10;
            m.Type = 0;
            m.FileSpectra = "testD1";
            m.Assistant = 150562;
            m.Note = "bdrum-test";

            _d1.LoadMeasurementInfoToDevice(m, sd);
            Assert.AreEqual(m.Duration, (int)_d1.PresetRealTime);

            _d1.Start();
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
            
            Assert.AreEqual(DetectorStatus.busy, _d1.Status);
            Assert.AreNotEqual(0, _d1.ElapsedRealTime);
            _d1.Pause();

            Assert.AreEqual(DetectorStatus.ready, _d1.Status);
            Assert.AreEqual(2f, (float)_d1.ElapsedRealTime, 1.0f);
            _d1.Start();

            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.AreEqual(DetectorStatus.busy, _d1.Status);
            Assert.AreNotEqual(2f, (float)_d1.ElapsedRealTime, 0.5f);

            _d1.Pause();
            Assert.AreEqual(m.Duration, (int)_d1.PresetRealTime);

            _d1.Save();
            Assert.IsTrue(File.Exists(_d1.FullFileSpectraName));
            Assert.AreEqual(Path.Combine(Path.GetDirectoryName(_d1.FullFileSpectraName),$"{m.FileSpectra}.cnf"), _d1.FullFileSpectraName);

            _d1.Save();
            Assert.IsTrue(File.Exists(_d1.FullFileSpectraName));
            Assert.AreEqual(Path.Combine(Path.GetDirectoryName(_d1.FullFileSpectraName), $"{m.FileSpectra}(1).cnf"), _d1.FullFileSpectraName);

            _d1.Save();
            Assert.IsTrue(File.Exists(_d1.FullFileSpectraName));
            Assert.AreEqual(Path.Combine(Path.GetDirectoryName(_d1.FullFileSpectraName), $"{m.FileSpectra}(2).cnf"), _d1.FullFileSpectraName);

            Assert.AreEqual(DetectorStatus.ready, _d1.Status);

            Assert.IsTrue(File.Exists(_d1.FullFileSpectraName));
            Assert.AreEqual(m.Duration, (int)_d1.PresetRealTime);
            _d1.Stop();

            f1.Open(_d1.FullFileSpectraName);

            Assert.AreEqual($"{_d1.Sample.SampleKey}",                     f1.Param[CanberraDataAccessLib.ParamCodes.CAM_T_STITLE].ToString()); // title
            Assert.AreEqual(_d1.Sample.Assistant,                          f1.Param[CanberraDataAccessLib.ParamCodes.CAM_T_SCOLLNAME].ToString()); // operator's name
            Assert.AreEqual(_d1.Sample.Note,                               $"{f1.Param[CanberraDataAccessLib.ParamCodes.CAM_T_SDESC1].ToString()} {f1.Param[CanberraDataAccessLib.ParamCodes.CAM_T_SDESC2].ToString()}  ");
            Assert.AreEqual(_d1.Sample.SampleCode,                             f1.Param[CanberraDataAccessLib.ParamCodes.CAM_T_SIDENT].ToString()); // sd code
            Assert.AreEqual(_d1.Sample.Weight.ToString(), f1.Param[CanberraDataAccessLib.ParamCodes.CAM_F_SQUANT].ToString()); // weight
            Assert.AreEqual("0",                                                       f1.Param[CanberraDataAccessLib.ParamCodes.CAM_F_SQUANTERR].ToString()); // err, 0
            Assert.AreEqual("gram",                                                    f1.Param[CanberraDataAccessLib.ParamCodes.CAM_T_SUNITS].ToString()); // units, gram
            Assert.AreEqual(_d1.Sample.DateTimeStart.ToString().Replace(" ", ""), f1.Param[CanberraDataAccessLib.ParamCodes.CAM_X_SDEPOSIT].ToString().Replace(" ", "")); // irr start date time
            Assert.AreEqual(_d1.Sample.DateTimeFinish.ToString().Replace(" ", ""), f1.Param[CanberraDataAccessLib.ParamCodes.CAM_X_STIME].ToString().Replace(" ", "")); // irr finish date time
            Assert.AreEqual("0",                                                       f1.Param[CanberraDataAccessLib.ParamCodes.CAM_F_SSYSERR].ToString()); // Random sd error (%)
            Assert.AreEqual("0",                                                       f1.Param[CanberraDataAccessLib.ParamCodes.CAM_F_SSYSTERR].ToString()); // Non-random sd error 
            Assert.AreEqual(_d1.Sample.Type,                               f1.Param[CanberraDataAccessLib.ParamCodes.CAM_T_STYPE].ToString());
            var s = f1.Param[CanberraDataAccessLib.ParamCodes.CAM_T_SGEOMTRY].ToString();
            Assert.AreEqual(_d1.Sample.Height,                       float.Parse(f1.Param[CanberraDataAccessLib.ParamCodes.CAM_T_SGEOMTRY].ToString()));

            Assert.AreEqual(_d1.PresetRealTime, uint.Parse(f1.Param[CanberraDataAccessLib.ParamCodes.CAM_X_PREAL].ToString())); // irr start date time
            Assert.AreEqual(m.Duration, int.Parse(f1.Param[CanberraDataAccessLib.ParamCodes.CAM_X_PREAL].ToString())); // irr start date time

            f1.Close();

            File.Delete(_d1.FullFileSpectraName);
            File.Delete(Path.Combine(Path.GetDirectoryName(_d1.FullFileSpectraName), $"{m.FileSpectra}(1).cnf"));
            File.Delete(Path.Combine(Path.GetDirectoryName(_d1.FullFileSpectraName), $"{m.FileSpectra}.cnf"));
            
            Assert.AreEqual(m.Duration, (int)_d1.PresetRealTime);

            Assert.IsFalse(File.Exists(_d1.FullFileSpectraName));
            Assert.IsFalse(File.Exists(Path.Combine(Path.GetDirectoryName(_d1.FullFileSpectraName), $"{m.FileSpectra}.cnf")));
            Assert.IsFalse(File.Exists(Path.Combine(Path.GetDirectoryName(_d1.FullFileSpectraName), $"{m.FileSpectra}(1).cnf")));
        }

    } // public class DetectorsTest
}     // namespace Tests

