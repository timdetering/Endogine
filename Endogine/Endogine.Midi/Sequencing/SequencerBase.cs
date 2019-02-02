/*
 * Created by: Leslie Sanford
 * 
 * Contact: jabberdabber@hotmail.com
 * 
 * Last modified: 09/27/2004
 */

using System;
using System.Collections;
using System.Diagnostics;

//Where's the sematic boundary between Sequencer and SequencePlayer..? Doesn't feel particularly well-defined.

namespace Endogine.Midi
{
	/// <summary>
	/// Plays back sequences.
	/// </summary>
	public class SequencerBase // : System.ComponentModel.Component
	{
		#region Fields

		// The sequence position.
		private int position = 0;

		// Indicates whether or not the sequencer should seek the position 
		// before beginning playback.
		private bool seekPosition = false;

		// The sequence to playback.
		private Sequence seq;

		// The version of the sequence.
		private int sequenceVersion;

		// The sequence player for controlling sequence playback.
		private SequencePlayer player;

		// The MIDI clock for controlling playback timing.
		private SlaveClock clock;

		// The tick generator used for timing the playback of MIDI messages.
		private TickGenerator tickGen;

		//TODO: Why can't I start with underscore???
		protected Midi.IMidiSender xoutDevice;

		//private float _tempoFactor = 1;
		#endregion

		#region Events

		/// <summary>
		/// Occurs when the tempo changes.
		/// </summary>
		public event EventHandler TempoChanged;

		#endregion

		#region Construction


		/// <summary>
		/// Initializes an instance of the Sequencer class.
		/// </summary>
		public SequencerBase(Midi.IMidiReceiver inDevice, Midi.IMidiSender outDevice)
		{
			this.tickGen = new Endogine.Midi.TickGenerator();
			this.tickGen.Ppqn = 96;
			//this.tickGen.Tempo = 500000;
			this.tickGen.Tick += new System.EventHandler(this.TickHandler);

			this.xoutDevice = outDevice;
			InitializeSequencer();
		}

		#endregion

		#region Methods

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		public void Dispose()
		{
			tickGen.TempoChanged -= new EventHandler(OnTempoChanged);
			tickGen.Dispose();
			this.player.Dispose();
			clock.Dispose();
		}


		/// <summary>
		/// Initializes the sequencer.
		/// </summary>
		protected virtual void InitializeSequencer()
		{
			seq = new Sequence();
			sequenceVersion = seq.Version;
			player = new SequencePlayer(this.xoutDevice, tickGen, seq);
			player.EndOfSequenceReached += 
				new EventHandler(EndOfSequenceReachedHandler);
			clock = new SlaveClock(null, this.xoutDevice, tickGen);

			clock.Starting += new EventHandler(StartingHandler);
			clock.Continuing += new EventHandler(ContinuingHandler);
			clock.Stopping += new EventHandler(StoppingHandler);
			clock.PositionChanged += new PositionChangedEventHandler(PositionChangedHandler);

			tickGen.TempoChanged += new EventHandler(OnTempoChanged);
		}

		/// <summary>
		/// Starts playback at the beginning of the sequence.
		/// </summary>
		public void Start()
		{
			clock.Start();
		}

		/// <summary>
		/// Continues playback from the current position.
		/// </summary>
		public void Continue()
		{
			// Guard.
			if(Position >= Sequence.Length)
				return;

			clock.Continue();
		}

		/// <summary>
		/// Stops playback.
		/// </summary>
		public void Stop()
		{
			clock.Stop();
		}

		/// <summary>
		/// Attaches a delegate to the internal tick generator the sequencer
		/// uses for timing playback.
		/// </summary>
		/// <param name="handler">
		/// The delegate to attach to the tick generator.
		/// </param>
		/// <remarks>
		/// Attaching a delegate to a sequencer's tick generator allows a 
		/// client to synchronize itself with the sequencer's playback.
		/// </remarks>
		public void AttachToTickGenerator(EventHandler handler)
		{
			tickGen.Tick += handler;
		}

		/// <summary>
		/// Detaches a delegate from the internal tick generator the sequencer
		/// uses for timing playback.
		/// </summary>
		/// <param name="handler">
		/// The delegate to detach from the tick generator.
		/// </param>
		public void DetachFromTickGenerator(EventHandler handler)
		{
			tickGen.Tick -= handler;
		}

		/// <summary>
		/// Sets the mute state of a track.
		/// </summary>
		/// <param name="index">
		/// The index into the sequence of the track to mute.
		/// </param>
		/// <param name="mute">
		/// A value indicating whether or not to mute the track.
		/// </param>
		public void MuteTrack(int index, bool mute)
		{
			player.MuteTrack(index, mute);
		}

		/// <summary>
		/// Sets the solo state of a track.
		/// </summary>
		/// <param name="index">
		/// The index into the sequence of the track to solo.
		/// </param>
		/// <param name="solo">
		/// A value indicating whether or not to solo the track.
		/// </param>
		public void SoloTrack(int index, bool solo)
		{
			player.SoloTrack(index, solo);
		}

		/// <summary>
		/// Handles tick events generated by the tick generator.
		/// </summary>
		/// <param name="sender">
		/// The tick generator responsible for the event.
		/// </param>
		/// <param name="e">
		/// Information about the event.
		/// </param>
		private void TickHandler(object sender, EventArgs e)
		{
			position++;
		}

		/// <summary>
		/// Handles the starting event generated by the MIDI clock.
		/// </summary>
		/// <param name="sender">
		/// The MIDI clock responsible for the event.
		/// </param>
		/// <param name="e">
		/// Information about the event.
		/// </param>
		protected virtual void StartingHandler(object sender, EventArgs e)
		{
			// Lock the sequence so that it cannot be modified during playback.
			Sequence.LockSequence(this, true);

//			outDevice.Reset();

			if(sequenceVersion != Sequence.Version)
			{
				PreparePlayer();
				sequenceVersion = Sequence.Version;
			}

			Position = 0;
			player.Seek(0);
		}

		/// <summary>
		/// Handles the continuing event generated by the MIDI clock.
		/// </summary>
		/// <param name="sender">
		/// The MIDI clock responsible for the event.
		/// </param>
		/// <param name="e">
		/// Information about the event.
		/// </param>
		private void ContinuingHandler(object sender, EventArgs e)
		{
			// Lock the sequence so that it cannot be modified during playback.
			Sequence.LockSequence(this, true);

			if(sequenceVersion != Sequence.Version)
			{
				PreparePlayer();
				sequenceVersion = Sequence.Version;
				seekPosition = true;
			}

			if(seekPosition)
			{
				player.Seek(position);
				seekPosition = false;
			}
		}

		/// <summary>
		/// Handles the stopping event generated by the MIDI clock.
		/// </summary>
		/// <param name="sender">
		/// The MIDI clock responsible for the event.
		/// </param>
		/// <param name="e">
		/// Information about the event.
		/// </param>
		private void StoppingHandler(object sender, EventArgs e)
		{
			// Unlock the sequence so that it can not be modified.
			Sequence.LockSequence(this, false);

			player.AllSoundsOff();
			seekPosition = false;
		}

		/// <summary>
		/// Handles the position changed event generated by the MIDI clock.
		/// </summary>
		/// <param name="sender">
		/// The MIDI clock responsible for the event.
		/// </param>
		/// <param name="e">
		/// Information about the event.
		/// </param>
		private void PositionChangedHandler(object sender, PositionChangedEventArgs e)
		{
			position = e.Position;
			player.Seek(position);
		}

		/// <summary>
		/// Handles the end of sequence reached event generated by the sequence 
		/// player.
		/// </summary>
		/// <param name="sender">
		/// The sequence player responsible for the event.
		/// </param>
		/// <param name="e">
		/// Information about the event.
		/// </param>
		private void EndOfSequenceReachedHandler(object sender, EventArgs e)
		{
			clock.Stop();
		}

		/// <summary>
		/// Handles and raises the tempo changed event.
		/// </summary>
		/// <param name="sender">
		/// The tick generator responsible for the event.
		/// </param>
		/// <param name="e">
		/// Information about the event.
		/// </param>
		private void OnTempoChanged(object sender, EventArgs e)
		{
			if(TempoChanged != null)
				TempoChanged(this, EventArgs.Empty);
		}        

		/// <summary>
		/// Prepares the sequence player for playback.
		/// </summary>
		private void PreparePlayer()
		{
			player.EndOfSequenceReached -= 
				new EventHandler(EndOfSequenceReachedHandler);
			player.Dispose();
			player = new SequencePlayer(this.xoutDevice, tickGen, seq); //outDevice, tickGen, seq
			player.EndOfSequenceReached += 
				new EventHandler(EndOfSequenceReachedHandler);
		}

		#endregion

		#region Properties


		/// <summary>
		/// Gets or sets a value indicating whether or not the master mode
		/// is enabled.
		/// </summary>
		public bool MasterEnabled
		{
			get
			{
				return clock.MasterEnabled;
			}
			set
			{
				clock.MasterEnabled = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not the slave mode is 
		/// enabled.
		/// </summary>
		public bool SlaveEnabled
		{
			get
			{
				return clock.SlaveEnabled;
			}
			set
			{
				clock.SlaveEnabled = value;
			}
		}

		/// <summary>
		/// Gets or sets the playback position in ticks.
		/// </summary>
		/// <remarks>
		/// Attempting to set the position while the slave mode is enabled has
		/// no effect.
		/// </remarks>
		public int Position
		{
			get
			{
				return position;
			}
			set
			{
				// Attempting to set the position in slave mode has no effect.
				if(SlaveEnabled)
					return;

				if(value < 0)
					throw new ArgumentOutOfRangeException("Positon", value,
						"Position out of range.");

				bool wasRunning = clock.IsRunning();
                
				if(wasRunning)
					clock.Stop();

				position = value;
				seekPosition = true;

				if(wasRunning)
					clock.Continue();
			}
		}

		/// <summary>
		/// Gets or sets the song position pointer.
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		public int SongPositionPointer
		{
			get
			{
				SongPositionPointer spp = 
					new SongPositionPointer(tickGen.Ppqn);

				spp.PositionInTicks = Position;

				return spp.SongPosition;
			}
			set
			{
				// Attempting to set the position in slave mode has no effect.
				if(SlaveEnabled)
					return;

				if(value < 0)
					throw new ArgumentOutOfRangeException("SongPositionPointer", 
						value, "Song position pointer out of range.");

				SongPositionPointer spp = 
					new SongPositionPointer(tickGen.Ppqn);

				spp.SongPosition = value;

				bool wasRunning = clock.IsRunning();
                
				if(wasRunning)
					clock.Stop();

				position = spp.PositionInTicks;
				seekPosition = true;

				clock.SendSongPositionPointer(spp);

				if(wasRunning)
					clock.Continue();
			}
		}

		/// <summary>
		/// Gets or sets the sequence to play back.
		/// </summary>
		public Sequence Sequence
		{
			get
			{
				return seq;
			}
			set
			{               
				Stop();

				seq = value;
				tickGen.Ppqn = seq.Division;   
				PreparePlayer();
				sequenceVersion = seq.Version;
				Position = 0;
			}
		}

		//JB
		/// <summary>
		/// Gets the SequencePlayer
		/// </summary>
		public SequencePlayer Player
		{
			get {return this.player;}
		}

		/// <summary>
		/// Gets or sets the tempo in BPM (old:microseconds)
		/// </summary>
		public float Tempo
		{
			get
			{
				return tickGen.Tempo; //60000000f/tickGen.Tempo/this._tempoFactor; //60f*tickGen.Tempo/1000000*4;
			}
			set
			{
				tickGen.Tempo = value; // (int)(60000000f/(value*this._tempoFactor)); //value*1000000/4/60);
			}
		}
		public float PlaybackSpeed
		{
			get {return this.tickGen.PlaybackSpeed;}
			set {this.tickGen.PlaybackSpeed = value;}
		}
		public float PlaybackTempo
		{
			get {return this.tickGen.PlaybackTempo;}
		}

		#endregion        
	}
}
