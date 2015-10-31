///*
// * Created by: Leslie Sanford
// * 
// * Contact: jabberdabber@hotmail.com
// * 
// * Last modified: 06/26/2004
// */
//
//using System;
//using System.ComponentModel;
//using System.Collections;
//using System.Diagnostics;
//using Multimedia;
//
//namespace Endogine.Midi
//{
//	/// <summary>
//	/// Generates ticks for Midi timing.
//	/// </summary>
//	public class TickGenerator : Component
//	{
//        #region TickGenerator Members
//
//        #region Constants
//
//        /// <summary>
//        /// The minimum tempo value allowed (250 bpm).
//        /// </summary>
//        public const int TempoMin = 240000;
//
//        /// <summary>
//        /// The maximum tempo value allowed (8 bpm).
//        /// </summary>
//        public const int TempoMax = 7500000;
//
//        /// <summary>
//        /// The minimum pulses per quarter note allowed.
//        /// </summary>
//        public const int PpqnMin = 24;
//
//        /// <summary>
//        /// The maximum pulses per quarter note allowed.
//        /// </summary>
//        public const int PpqnMax = 960;
//
//        // The default temp (120 bpm).
//        private const int DefaultTempo = 500000;
//
//        // The default pulses per quarter note.
//        private const int DefaultPpqn = 96;
//
//        #endregion
//
//        #region Fields
//
//        // Timer interrupt in microseconds.
//        private int period;
//
//        // Tempo in microseconds.
//        private int tempo;
//
//        // Pulses per quarter note.
//        private int pulsesPerQuarterNote;
//
//        // Whole part of the running tick calculation.
//        private int nTicks;
//
//        // Fractional part of the running tick calculation.
//        private int fTicks;
//
//        // Period interval multiplied by pulses per quarter note.
//        private int trTime;
//
//        // Provides timing events.
//        private Timer timer;        
//
//		/// <summary>
//		/// Required designer variable.
//		/// </summary>
//		private System.ComponentModel.Container components = null;
//
//        #endregion
//
//        /// <summary>
//        /// Occurs when a tick is generated.
//        /// </summary>
//        public event EventHandler Tick;
//
//        /// <summary>
//        /// 
//        /// </summary>
//        public event EventHandler TempoChanged;
//
//        #region Construction
//
//        /// <summary>
//        /// Initializes an instance of the TickGenerator class with the 
//        /// specified component container.
//        /// </summary>
//        /// <param name="container">
//        /// The container the tick generator should add itself to.
//        /// </param>
//		public TickGenerator(System.ComponentModel.IContainer container)
//		{
//			//
//			// Required for Windows.Forms Class Composition Designer support
//			//
//			container.Add(this);
//			InitializeComponent();
//
//            InitializeTickGenerator();            
//		}
//
//        /// <summary>
//        /// Initializes a new instance of the TickGenerator class.
//        /// </summary>
//		public TickGenerator()
//		{
//			//
//			// Required for Windows.Forms Class Composition Designer support
//			//
//			InitializeComponent();
//
//            InitializeTickGenerator();
//		}
//
//        #endregion
//
//        #region Methods
//
//		/// <summary> 
//		/// Clean up any resources being used.
//		/// </summary>
//		protected override void Dispose( bool disposing )
//		{
//            if(IsRunning())
//            {
//                Stop();
//            }
//
//			if( disposing )
//			{
//				if(components != null)
//				{
//					components.Dispose();
//				}
//			}
//
//			base.Dispose( disposing );
//		}
//
//		#region Component Designer generated code
//		/// <summary>
//		/// Required method for Designer support - do not modify
//		/// the contents of this method with the code editor.
//		/// </summary>
//		private void InitializeComponent()
//		{
//			components = new System.ComponentModel.Container();
//		}
//		#endregion
//
//        /// <summary>
//        /// Starts the tick generator.
//        /// </summary>
//        public void Start()
//        {
//            // If the tick generator is not already running.
//            if(!IsRunning())
//            {
//                // Start the timer.
//                timer.Start(); 
//            }
//        }
//
//        /// <summary>
//        /// Stops the tick generator.
//        /// </summary>
//        public void Stop()
//        {
//            // If the tick generator is running.
//            if(IsRunning())
//            {
//                // Stop the timer.
//                timer.Stop();
//            }
//        }
//
//        /// <summary>
//        /// Indicates wheter or not the tick generator is running.
//        /// </summary>
//        /// <returns>
//        /// <b>true</b> if the tick generator is running; otherwise, 
//        /// <b>false</b>.
//        /// </returns>
//        public bool IsRunning()
//        {
//            return timer.IsRunning();
//        }
//
//        /// <summary>
//        /// Initializes tick generator.
//        /// </summary>
//        private void InitializeTickGenerator()
//        {
//            TimerCaps caps = Timer.Capabilities;
//            timer = new Timer(new TimerCallback(OnTick), null, caps.periodMin, 
//                0, TimerMode.Periodic);
//            period = caps.periodMin * 1000;
//            Tempo = DefaultTempo;
//            pulsesPerQuarterNote = DefaultPpqn;
//        }
//
//        /// <summary>
//        /// Reset tick generator.
//        /// </summary>
//        private void Reset()
//        {
//            // Keeps track of whether or not the tick generator was running.
//            bool wasRunning = false;
//
//            // If the tick generator is running.
//            if(IsRunning())
//            {
//                // Stop tick generator.
//                Stop();
//
//                // Indicate that the tick generator was running.
//                wasRunning = true;
//            }
//
//            // Initialize tick variables.
//            nTicks = fTicks = 0;
//            trTime = period * pulsesPerQuarterNote;
//
//            // If the tick generator was running.
//            if(wasRunning)
//            {
//                // Start the tick generator again.
//                Start();
//            }
//        }
//
//        /// <summary>
//        /// Handles tick events from the multimedia timer.
//        /// </summary>
//        /// <param name="state">
//        /// State information (ignorned here).
//        /// </param>
//        private void OnTick(object state)
//        {
//            // Calculate whole part of running tick count.
//            nTicks = (fTicks + trTime) / tempo;
//
//            // Calculate fractional part of running tick count.
//            fTicks += trTime - nTicks * tempo;
//            
//            // While there are ticks.
//            while(nTicks > 0 && Tick != null)
//            {
//                // Trigger tick event.
//                Tick(this, EventArgs.Empty);
//
//                nTicks--;
//            }
//        }
//
//        #endregion
//
//        /// <summary>
//        /// Gets or sets the tempo in microseconds.
//        /// </summary>
//        public int Tempo
//        {
//            get
//            {
//                return tempo;
//            }
//            set
//            {
//                // Enforce preconditions.
//                if(value < TempoMin || value > TempoMax)
//                    throw new ArgumentOutOfRangeException("Tempo", value,
//                        "Tempo out of range.");
//
//                // Set the tempo.
//                tempo = value;
//
//                if(TempoChanged != null)
//                    TempoChanged(this, new EventArgs());
//            }
//        }
//
//        /// <summary>
//        /// Gets or sets the pulses per quarter note.
//        /// </summary>
//        public int Ppqn
//        {
//            get
//            {
//                return pulsesPerQuarterNote;
//            }
//            set
//            {
//                // Enforce preconditions.
//                if(value < PpqnMin || value > PpqnMax || 
//                    value % PpqnMin != 0)
//                    throw new ArgumentOutOfRangeException("Ppqn", value,
//                        "Pulses per quarter note out of range.");
//                
//                // Set the ticks per beat.
//                pulsesPerQuarterNote = value;
//
//                // Reset tick generator.
//                Reset();
//            }
//        }
//
//        #endregion
//    }
//}
