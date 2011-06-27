﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MidiShapeShifter.Mss.Mapping.MssMsgInfoTypes;

namespace MidiShapeShifter.Mss.Mapping
{
    public enum IoType { Input, Output };


    /// <summary>
    ///     A MappingEntry stores all information associated with a mapping. This information can be broken down into:
    ///     1. Information about which MSS messages will be accepted for input by the mapping (eg. InMssMsgInfo)
    ///     2. Information about how to modify incomeing MSS messages. OnMssMsgInfo is used to map the incoming MSS 
    ///         message's type, data1, and data2. The equation is used to map the incoming MSS message's data3.
    /// </summary>
    public class MappingEntry : ICurveShapeInfoContainer
    {
        /// <summary>
        ///     Specifies which MSS messages will be accepted for input as well as additional information about the 
        ///     input type
        /// </summary>
        public MssMsgInfo InMssMsgInfo;

        /// <summary>
        ///     Specifies the range of messages that can be output as well as additional information about the output 
        ///     type.
        /// </summary>
        public MssMsgInfo OutMssMsgInfo;

        /// <summary>
        ///     If there are multiple mapping entries with overlapping input ranges then a single mss message can
        ///     generate several messages. This can be disabled by setting the override duplicates flag to true.
        ///     If there are two mapping entries with an overlapping inMsgRange and overrideDuplicates is set to 
        ///     true for each one, then the one closer to the top of the mapping list box overrides the other.
        /// </summary>
        public bool OverrideDuplicates;

        /// <summary>
        ///     Contains information about the curve shape for this mapping and how it is being entered.
        /// </summary>
        public CurveShapeInfo CurveShapeInfo { get; set; }
        //public CurveShapeInfo CurveShapeInfo;


        public MappingEntry() 
        { 
        
        }

        /// <summary>
        ///     Gets a string representing an MssMsgType used by this MappingEntry
        /// </summary>
        /// <param name="ioCategory">Specifies wheather to use the type from InMssMsgInfo or OutMssMsgInfo</param>
        public string GetReadableMsgType(IoType ioCategory)
        {
            if (ioCategory == IoType.Input)
            {
                return MssMsg.MssMsgTypeNames[(int)this.InMssMsgInfo.mssMsgType];
            }
            else if (ioCategory == IoType.Output)
            {
                return MssMsg.MssMsgTypeNames[(int)this.OutMssMsgInfo.mssMsgType];
            }
            else
            {
                //Unknown IO type
                Debug.Assert(false);
                return "";
            }
        }

        /// <summary>
        ///     Gets a string representing this MappingEntry's OverrideDuplicates status
        /// </summary>
        public string GetReadableOverrideDuplicates()
        {
            if (this.OverrideDuplicates == true)
            {
                return "Yes";
            }
            else
            {
                return "No";
            }
        }
    }
}