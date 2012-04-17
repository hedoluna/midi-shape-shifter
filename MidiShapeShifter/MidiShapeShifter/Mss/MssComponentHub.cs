﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using MidiShapeShifter.Mss.Mapping;
using MidiShapeShifter.Mss.Mapping.MssMsgRangeEntryMetadataTypes;
using MidiShapeShifter.Mss.Generator;
using MidiShapeShifter.Mss.UI;
using MidiShapeShifter.Mss.Relays;
using MidiShapeShifter.Mss.MssMsgInfoTypes;
using MidiShapeShifter.Mss.Parameters;

namespace MidiShapeShifter.Mss
{
    /// <summary>
    ///     MssComponentHub manages all aspects of the plugin that do not rely on the jacobi framework. This class and 
    ///     all of its members do not have any references to the Jacobi framework or classes in the 
    ///     MidiShapeShifter.Framework namespace. This will make it much easier to extend this plugin to other 
    ///     frameworks or to a standalone application.
    /// </summary>
    [DataContract]
    public class MssComponentHub
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        protected SendMssEventsToHostTrigger sendEventsToHostTrigger;
        protected DryMssEventHandler dryMssEventHandler;
        [DataMember(Name = "MappingMgr")]
        protected MappingManager mappingMgr;

        protected MssEventGenerator mssEventGenrator;
        [DataMember(Name = "GenMappingMgr")]
        protected GeneratorMappingManager genMappingMgr;
        
        /// <summary>
        ///     Passes unprocessed MssEvents from the "Framework" namespace to the "Mss" namespace.
        /// </summary>
        protected DryMssEventRelay _dryMssEventRelay;
        public IDryMssEventInputPort DryMssEventInputPort { get { return this._dryMssEventRelay; } }
        public IDryMssEventOutputPort DryMssEventOutputPort { get { return this._dryMssEventRelay; } }

        /// <summary>
        ///     Passes processed MssEvents from the "Mss" namespace to the "Framework" namespace.
        /// </summary>
        protected WetMssEventRelay _wetMssEventRelay;
        public IWetMssEventInputPort WetMssEventInputPort { get { return this._wetMssEventRelay; } }
        public IWetMssEventOutputPort WetMssEventOutputPort { get { return this._wetMssEventRelay; } }

        /// <summary>
        ///     Passes information about the host from the "Framework" namespace to the "Mss" namespace
        /// </summary>
        protected HostInfoRelay _hostInfoRelay;
        public IHostInfoInputPort HostInfoInputPort { get { return this._hostInfoRelay; } }
        public IHostInfoOutputPort HostInfoOutputPort { get { return this._hostInfoRelay; } }

        [DataMember(Name = "MssParameters")]
        protected MssParameters _mssParameters;
        public MssParameters MssParameters { get { return this._mssParameters; } }

        [DataMember(Name = "MssProgramMgr")]
        protected MssProgramMgr _mssProgramMgr;
        public MssProgramMgr MssProgramMgr { get { return this._mssProgramMgr; } }

        protected TransformPresetMgr transformPresetMgr;

        protected Factory_MssMsgRangeEntryMetadata msgEntryMetadataFactory;
        protected IFactory_MssMsgInfo msgInfoFactory;

        protected PluginEditorView _pluginEditorView;
        public PluginEditorView PluginEditorView { 
            get 
            {
                EnsurePluginEditorExists();
                return this._pluginEditorView; 
            } 
        }

        [DataMember(Name = "PluginEditorInfo")]
        protected SerializablePluginEditorInfo pluginEditorInfo;

        public MssComponentHub()
        {
            ConstructNonSerializableMembers();

            //Construct Serializable members
            this._mssProgramMgr = new MssProgramMgr();

            this.mappingMgr = new MappingManager();
            this.genMappingMgr = new GeneratorMappingManager();

            this._mssParameters = new MssParameters();
            
            this.pluginEditorInfo = new SerializablePluginEditorInfo();
        }

        protected void ConstructNonSerializableMembers()
        {
            this.sendEventsToHostTrigger = new SendMssEventsToHostTrigger();
            this.dryMssEventHandler = new DryMssEventHandler();

            this._dryMssEventRelay = new DryMssEventRelay();
            this._wetMssEventRelay = new WetMssEventRelay();
            this._hostInfoRelay = new HostInfoRelay();

            this.mssEventGenrator = new MssEventGenerator();

            this.msgEntryMetadataFactory = new Factory_MssMsgRangeEntryMetadata();
            this.msgInfoFactory = new Factory_MssMsgInfo();

            transformPresetMgr = new TransformPresetMgr();
        }

        /// <summary>
        ///     Initialized members
        /// </summary>
        public void Init()
        {
            InitializeNonSerializableMembers();
            //Initialize serializable members
            this._mssParameters.Init();
            this.MssProgramMgr.Init();
        }

        protected void InitializeNonSerializableMembers()
        {
            this.msgEntryMetadataFactory.Init(this.genMappingMgr);
            this.msgInfoFactory.Init(this.genMappingMgr);

            this.sendEventsToHostTrigger.Init(this.HostInfoOutputPort, this.WetMssEventInputPort);
            this.dryMssEventHandler.Init(this.DryMssEventOutputPort, 
                                         this.WetMssEventInputPort, 
                                         this.mappingMgr, 
                                         this.MssParameters);
            this.mssEventGenrator.Init(this.HostInfoOutputPort,
                                       this.WetMssEventOutputPort,
                                       this.DryMssEventInputPort,
                                       this.genMappingMgr,
                                       this.MssParameters);

            transformPresetMgr.Init(this.pluginEditorInfo, this.mappingMgr, this.genMappingMgr);
        }


        [OnDeserializing]
        protected void OnDeserializing(StreamingContext context)
        {
            ConstructNonSerializableMembers();
        }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            InitializeNonSerializableMembers();
        }

        /// <summary>
        ///     Creates an displays the PluginEditorView.
        /// </summary>
        public void OpenPluginEditor(IntPtr hWnd)
        {
            EnsurePluginEditorExists();

            SetParent(this._pluginEditorView.Handle, hWnd);

            this._pluginEditorView.Show();
        }

        /// <summary>
        ///     Hides and disposes of the PluginEditorView.
        /// </summary>
        public void ClosePluginEditor()
        {
            if (this._pluginEditorView != null)
            {
                this._pluginEditorView.Dispose();
                this._pluginEditorView = null;
            }
        }

        /// <summary>
        ///     Creates and initializes the PluginEditorView if it does not already exist.
        /// </summary>
        protected void EnsurePluginEditorExists()
        {
            if (this._pluginEditorView == null)
            {
                this._pluginEditorView = new PluginEditorView();

                this._pluginEditorView.CreateControl();
                this._pluginEditorView.Init(this.MssParameters, 
                                            this.mappingMgr, 
                                            this.genMappingMgr,
                                            this.MssProgramMgr,
                                            this.transformPresetMgr,
                                            this.DryMssEventOutputPort,
                                            this.pluginEditorInfo);
            }
        }

    }
}
