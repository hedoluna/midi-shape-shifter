﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Jacobi.Vst.Core;

using MidiShapeShifter.Mss.Relays;

namespace MidiShapeShifter.Framework
{
    public class VstTimeInfoTransmitter
    {
        public static readonly VstTimeInfoFlags RequiredTimeInfoFlags = VstTimeInfoFlags.PpqPositionValid |
                                                                       VstTimeInfoFlags.TempoValid |
                                                                       VstTimeInfoFlags.TimeSignatureValid;

        protected IHostInfoInputPort hostInfoInputPort;

        public void Init(IHostInfoInputPort hostInfoInputPort)
        {
            this.hostInfoInputPort = hostInfoInputPort;
        }

        public void TransmitTimeInfoToRelay(VstTimeInfo timeInfo, long timeInfoCreatedSampleTime)
        {
            //Ensures that the host is transmitting all the required time info.
            Debug.Assert((timeInfo.Flags & RequiredTimeInfoFlags) == RequiredTimeInfoFlags);

            double quarterNotesPerBar = ((double)timeInfo.TimeSignatureNumerator / 
                                     (double)timeInfo.TimeSignatureDenominator) / 0.25;
            double barPos = timeInfo.PpqPosition / quarterNotesPerBar;

            this.hostInfoInputPort.StartUpdate();

            this.hostInfoInputPort.ReceiveSampleRateDuringUpdate(timeInfo.SampleRate);

            this.hostInfoInputPort.ReceiveTempoDuringUpdate(timeInfo.Tempo);

            this.hostInfoInputPort.ReceiveBarPositionDuringUpdate(barPos, timeInfoCreatedSampleTime);

            this.hostInfoInputPort.ReceiveTransportPlayingDuringUpdate(
                (timeInfo.Flags & VstTimeInfoFlags.TransportPlaying) != 0);

            this.hostInfoInputPort.ReceiveTimeSignatureDuringUpdate(timeInfo.TimeSignatureNumerator, 
                                                                    timeInfo.TimeSignatureDenominator);

            this.hostInfoInputPort.FinishUpdate();
        }
    }
}