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
    [Serializable]
    public class MssComponentHub
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        [NonSerialized]
        protected SendMssEventsToHostTrigger sendEventsToHostTrigger;
        [NonSerialized]
        protected DryMssEventHandler dryMssEventHandler;
        protected MappingManager mappingMgr;

        [NonSerialized]
        protected MssEventGenerator mssEventGenrator;
        protected GeneratorMappingManager genMappingMgr;
        
        /// <summary>
        ///     Passes unprocessed MssEvents from the "Framework" namespace to the "Mss" namespace.
        /// </summary>
        [NonSerialized]
        protected DryMssEventRelay _dryMssEventRelay;
        public IDryMssEventInputPort DryMssEventInputPort { get { return this._dryMssEventRelay; } }
        public IDryMssEventOutputPort DryMssEventOutputPort { get { return this._dryMssEventRelay; } }

        /// <summary>
        ///     Passes processed MssEvents from the "Mss" namespace to the "Framework" namespace.
        /// </summary>
        [NonSerialized]
        protected WetMssEventRelay _wetMssEventRelay;
        public IWetMssEventInputPort WetMssEventInputPort { get { return this._wetMssEventRelay; } }
        public IWetMssEventOutputPort WetMssEventOutputPort { get { return this._wetMssEventRelay; } }

        /// <summary>
        ///     Passes information about the host from the "Framework" namespace to the "Mss" namespace
        /// </summary>
        [NonSerialized]
        protected HostInfoRelay _hostInfoRelay;
        public IHostInfoInputPort HostInfoInputPort { get { return this._hostInfoRelay; } }
        public IHostInfoOutputPort HostInfoOutputPort { get { return this._hostInfoRelay; } }

        protected MssParameters _mssParameters;
        public MssParameters MssParameters { get { return this._mssParameters; } }

        [OptionalField(VersionAdded = 2)]
        protected MssProgramMgr _mssProgramMgr;
        public MssProgramMgr MssProgramMgr { get { return this._mssProgramMgr; } }

        [NonSerialized]
        protected TransformPresetMgr transformPresetMgr;


        [NonSerialized]
        protected Factory_MssMsgRangeEntryMetadata msgEntryMetadataFactory;
        [NonSerialized]
        protected IFactory_MssMsgInfo msgInfoFactory;

        [NonSerialized]
        protected PluginEditorView _pluginEditorView;
        public PluginEditorView PluginEditorView { 
            get 
            {
                EnsurePluginEditorExists();
                return this._pluginEditorView; 
            } 
        }

        protected SerializablePluginEditorInfo pluginEditorInfo;

        public MssComponentHub()
        {
            ConstructNonSerializableMembers();
            ConstructOptionallySerializedMembers();

            //Construct Serializable members
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

        protected void ConstructOptionallySerializedMembers()
        {
            this._mssProgramMgr = new MssProgramMgr();
        }

        /// <summary>
        ///     Initialized members
        /// </summary>
        public void Init()
        {
            InitializeNonSerializableMembers();
            InitializeOptionallySerializedMembers();
            //Initialize serializable members
            this._mssParameters.Init();
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

        protected void InitializeOptionallySerializedMembers()
        {
            this.MssProgramMgr.Init();
        }

        [OnDeserializing]
        protected void OnDeserializing(StreamingContext context)
        {
            ConstructNonSerializableMembers();
            ConstructOptionallySerializedMembers();
            InitializeOptionallySerializedMembers();
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
