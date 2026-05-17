using Godot;
using System;
using System.Threading.Tasks;

public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; } = null!;  

	public AudioStreamPlayer BGMPlayer { get; private set; } = null!;
	public Godot.Collections.Array<AudioStreamPlayer> SFXPlayers { get; private set; } = new Godot.Collections.Array<AudioStreamPlayer>();

	[Export] public Godot.Collections.Dictionary<string, AudioStream> SFXStreams { get; private set; } = new Godot.Collections.Dictionary<string, AudioStream>();
	
	[Export] public AudioStream BGM { get; private set; } = null!;

	[Export] public float BGMFilterFrequency { get; private set; } = 1000f;

	[Export] public float BGMFilterTime { get; private set; } = 1f;

	private bool _isBGMFiltered = false;
	public override void _Ready()
	{
		Instance = this;
		BGMPlayer = new AudioStreamPlayer();
		AddChild(BGMPlayer);
		BGMPlayer.Bus = "BGM";
		BGMPlayer.Stream = BGM;

		// Ensure the BGM bus has a LowPassFilter effect for the filter/unfilter feature
		int bgmBusIndex = AudioServer.GetBusIndex("BGM");
		if (AudioServer.GetBusEffectCount(bgmBusIndex) == 0)
		{
			AudioEffectLowPassFilter lowPassFilter = new AudioEffectLowPassFilter();
			lowPassFilter.CutoffHz = 22000f; // fully open by default
			AudioServer.AddBusEffect(bgmBusIndex, lowPassFilter);
		}
		AudioServer.SetBusBypassEffects(bgmBusIndex, true);

		for (int i = 0; i < 20; i++)
		{
			AudioStreamPlayer sfxPlayer = new AudioStreamPlayer();
			sfxPlayer.Bus = "SFX";
			AddChild(sfxPlayer);
			SFXPlayers.Add(sfxPlayer);
		}

		


		PlayBGM();
	}

	public void PlayBGM()
	{
		if (BGMPlayer.Stream != null)
		{
			BGMPlayer.Play();
		}
	}

	public void PlaySFX(string sfxName)
	{
		if (SFXStreams.TryGetValue(sfxName, out var stream))
		{
			foreach (var sfxPlayer in SFXPlayers)
			{
				if (!sfxPlayer.Playing)
				{
					sfxPlayer.Stream = stream;
					sfxPlayer.Play();
					break;
				}
			}
		}
		else
		{
			Debug.PrintErr($"SFX '{sfxName}' not found in AudioManager.");
		}
	}

	public async Task FilterBGM(float time = 1f)
	{
		Tween tween = CreateTween();
		AudioServer.SetBusBypassEffects(AudioServer.GetBusIndex("BGM"), false);
		AudioEffectLowPassFilter audioEffectLowPassFilter = AudioServer.GetBusEffect(AudioServer.GetBusIndex("BGM"), 0) as AudioEffectLowPassFilter;
		tween.TweenProperty(audioEffectLowPassFilter, "cutoff_hz", BGMFilterFrequency, time).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	public async Task UnfilterBGM(float time = 1f)
	{
		Tween tween = CreateTween();
		AudioEffectLowPassFilter audioEffectLowPassFilter = AudioServer.GetBusEffect(AudioServer.GetBusIndex("BGM"), 0) as AudioEffectLowPassFilter;
		tween.TweenProperty(audioEffectLowPassFilter, "cutoff_hz", 22000f, time).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		await ToSignal(tween, Tween.SignalName.Finished);
		AudioServer.SetBusBypassEffects(AudioServer.GetBusIndex("BGM"), true);
	}

	public async Task ToggleBGMFilter()
	{
		if (_isBGMFiltered)
		{   
			await UnfilterBGM();
		}
		else
		{
			await FilterBGM();
		}
		_isBGMFiltered = !_isBGMFiltered;
	}

	public void SetBGMVolume(float volume)
	{
		SetBusVolume("BGM", volume);
	}

	public void SetSFXVolume(float volume)
	{
		SetBusVolume("SFX", volume);
	}

	private static void SetBusVolume(string busName, float volume)
	{
		int busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex == -1)
		{
			GD.PushWarning($"Audio bus '{busName}' not found.");
			return;
		}

		AudioServer.SetBusVolumeDb(busIndex, volume);
	}
}

