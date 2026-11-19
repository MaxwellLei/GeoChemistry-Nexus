using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace GeoChemistryNexus.Controls.Startup;

public sealed class StartupAnimation : FrameworkElement
{
	private sealed record Grain(Point Position, int Mineral, double Angle);

	private sealed record Particle(Point Rock, Color RockColor, Color ElementColor, Point3D Crystal, Color SiteColor, Point Diagram, Point Logo, Color FaceColor, double Delay, double Curve, double Size, double Phase);

	private static readonly Color Ink = Color.FromRgb(22, 32, 74);

	private static readonly Color Sub = Color.FromRgb(107, 115, 146);

	private static readonly Color Brand = Color.FromRgb(60, 74, 152);

	private static readonly Color[] Faces = new Color[3]
	{
		Rgb(78, 94, 196),
		Rgb(44, 56, 128),
		Rgb(110, 128, 226)
	};

	private static readonly Color[] Sites = new Color[2]
	{
		Rgb(47, 107, byte.MaxValue),
		Rgb(230, 126, 34)
	};

	private static readonly Point[][] Raw = new Point[3][]
	{
		new Point[6]
		{
			P(70.0, 38.0),
			P(137.0, 38.0),
			P(128.0, 93.0),
			P(53.0, 144.0),
			P(19.0, 132.0),
			P(20.0, 98.0)
		},
		new Point[4]
		{
			P(152.0, 37.0),
			P(175.0, 105.0),
			P(160.0, 120.0),
			P(141.0, 94.0)
		},
		new Point[4]
		{
			P(131.0, 105.0),
			P(148.0, 127.0),
			P(106.0, 159.0),
			P(69.0, 148.0)
		}
	};

	private static readonly Point A = P(0.0, -243.88);

	private static readonly Point F = P(-220.0, 137.16);

	private static readonly Point M = P(220.0, 137.16);

	private const double TextScale = 1.3;

	private const double BrandTextScale = 1.1;

	private const double SceneContentScale = 0.9;

	private const double SceneContentAnchorY = -10.0;

	private StartupText _copy = StartupText.Create();

	private static readonly Color[] MineralColors = new Color[6]
	{
		Rgb(214, 218, 222),
		Rgb(226, 178, 160),
		Rgb(236, 232, 222),
		Rgb(150, 108, 72),
		Rgb(118, 142, 110),
		Rgb(78, 78, 88)
	};

	private static readonly string[][] MineralElements = new string[6][]
	{
		new string[4] { "Si", "Si", "Si", "O" },
		new string[4] { "K", "Al", "Si", "Si" },
		new string[4] { "Na", "Ca", "Al", "Si" },
		new string[4] { "Fe", "Mg", "K", "Fe" },
		new string[4] { "Ca", "Fe", "Mg", "Mg" },
		new string[4] { "Fe", "Fe", "Ti", "Fe" }
	};

	private static readonly Dictionary<string, Color> Elements = new Dictionary<string, Color>
	{
		["Si"] = Rgb(31, 122, 224),
		["O"] = Rgb(120, 130, 150),
		["Al"] = Rgb(123, 92, 214),
		["K"] = Rgb(214, 64, 122),
		["Na"] = Rgb(214, 150, 0),
		["Ca"] = Rgb(224, 123, 26),
		["Fe"] = Rgb(214, 69, 47),
		["Mg"] = Rgb(46, 157, 91),
		["Ti"] = Rgb(93, 107, 214)
	};

	private readonly StartupTimeline _timeline = new StartupTimeline();

	private readonly TaskCompletionSource<bool> _firstFrame = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly TaskCompletionSource<bool> _completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly Random _random = new Random(20261114);

	private readonly Point[][] _rock = Facets(2.15, -14.0);

	private readonly Point[][] _mark = Facets(1.45, -78.0);

	private readonly List<Grain> _grains = new List<Grain>();

	private readonly List<Particle> _particles = new List<Particle>();

	private readonly List<Point3D> _sites = new List<Point3D>();

	private readonly List<(int A, int B)> _bonds = new List<(int, int)>();

	private readonly Dictionary<uint, SolidColorBrush> _brushes = new Dictionary<uint, SolidColorBrush>();

	private readonly Dictionary<(string Text, double Size, bool Bold, bool Mono, double Width, bool Wrap, int Align), FormattedText> _texts = new Dictionary<(string, double, bool, bool, double, bool, int), FormattedText>();

	private readonly Stopwatch _clock = new Stopwatch();

	private readonly DrawingGroup _rockTexture;

	private readonly Geometry[] _markGeometry;

	private double _lastTime;

	private bool _running;

	private string? _status;

	internal double SceneProgress => _timeline.Progress;

	internal double LoadingProgress => _timeline.LoadingProgress;

	internal long FrameCount { get; private set; }

	public Task FirstFrame => _firstFrame.Task;

	public StartupAnimation()
	{
		ClipToBounds = true;
		Focusable = false;
		_rockTexture = LoadRockTexture();
		_markGeometry = _mark.Select((Point[] p) => RoundedPolygon(p, 5.8)).ToArray();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Start();
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			Stop();
		};
	}

	private void PrepareScene()
	{
		if (!_running)
		{
			return;
		}
		int[] array = new int[11]
		{
			0, 0, 0, 1, 1, 2, 2, 2, 3, 4,
			5
		};
		for (int i = 0; i < 72; i++)
		{
			_grains.Add(new Grain(RandomIn(_rock), array[_random.Next(array.Length)], _random.NextDouble() * Math.PI));
		}
		for (int j = 0; j < 5; j++)
		{
			for (int k = 0; k < 5; k++)
			{
				for (int l = 0; l < 5; l++)
				{
					_sites.Add(new Point3D((j - 2) * 58, (k - 2) * 58, (l - 2) * 58));
				}
			}
		}
		for (int m = 0; m < _sites.Count; m++)
		{
			for (int n = m + 1; n < _sites.Count; n++)
			{
				if ((_sites[m] - _sites[n]).Length < 58.6)
				{
					_bonds.Add((m, n));
				}
			}
		}
		BuildParticles();
	}

	public void ReloadText()
	{
		((DispatcherObject)this).VerifyAccess();
		_copy = StartupText.Create();
		_status = null;
		_texts.Clear();
		InvalidateVisual();
	}

	public void ReportProgress(double percent, StartupStatus status)
	{
		ReportProgress(percent, _copy.Status(status));
	}

	public void ReportProgress(double percent, string? status)
	{
		((DispatcherObject)this).VerifyAccess();
		_timeline.Report(percent);
		if (!string.IsNullOrWhiteSpace(status))
		{
			_status = status;
		}
	}

	public Task FinishAsync()
	{
		((DispatcherObject)this).VerifyAccess();
		_timeline.Finish();
		return _completed.Task;
	}

	internal void Stop()
	{
		if (_running)
		{
			CompositionTarget.Rendering -= OnRendering;
		}
		_running = false;
		_completed.TrySetResult(result: false);
		_firstFrame.TrySetResult(result: false);
	}

	private void Start()
	{
		if (!_running)
		{
			_running = true;
			_clock.Start();
			CompositionTarget.Rendering += OnRendering;
			InvalidateVisual();
		}
	}

	private void OnRendering(object? sender, EventArgs e)
	{
		double totalSeconds = _clock.Elapsed.TotalSeconds;
		_timeline.Advance(Math.Min(0.1, totalSeconds - _lastTime));
		_lastTime = totalSeconds;
		InvalidateVisual();
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		if (!(ActualWidth <= 0.0) && !(ActualHeight <= 0.0))
		{
			dc.DrawRectangle(Brushes.White, null, new Rect(RenderSize));
			double num = Math.Min(ActualWidth / 1150.0, ActualHeight / 720.0);
			dc.PushTransform(new TranslateTransform(ActualWidth / 2.0, ActualHeight / 2.0));
			dc.PushTransform(new ScaleTransform(num, num));
			dc.PushOpacity(_timeline.Opacity);
			DrawScene(dc, _timeline.Progress, _clock.Elapsed.TotalSeconds, ActualWidth / num, ActualHeight / num);
			dc.Pop();
			dc.Pop();
			dc.Pop();
			FrameCount++;
			if (_firstFrame.TrySetResult(result: true))
			{
				((DispatcherObject)this).Dispatcher.BeginInvoke((DispatcherPriority)4, (Delegate)new Action(PrepareScene));
			}
			if (_timeline.IsComplete)
			{
				CompositionTarget.Rendering -= OnRendering;
				_running = false;
				_completed.TrySetResult(result: true);
			}
		}
	}

	private void DrawScene(DrawingContext dc, double progress, double time, double width, double height)
	{
		int stage = Math.Min(4, (int)(progress * 5.0));
		double u = ((progress >= 1.0) ? 1.0 : (progress * 5.0 - (double)stage));
		DrawGrid(dc, width, height);
		dc.PushTransform(new ScaleTransform(0.9, 0.9, 0.0, -10.0));
		(Point, double)[] sites = _sites.Select((Point3D s) => Project(s, time)).ToArray();
		if (stage == 0)
		{
			double num = Lerp(1.04, 1.0, Ease(Clamp(u / 0.55)));
			dc.PushTransform(new ScaleTransform(num, num, 0.0, -14.0));
			DrawRock(dc, 1.0);
			dc.Pop();
		}
		double num2 = Lerp(-188.0, 188.0, Clamp((u - 0.02) / 0.8));
		if (stage == 1)
		{
			double num3 = 1.0 - Ease(Clamp((u - 0.8) / 0.2));
			DrawRock(dc, 0.1 * num3);
			dc.PushClip(new RectangleGeometry(new Rect(num2, -200.0, 600.0, 420.0)));
			DrawRock(dc, num3);
			dc.Pop();
		}
		if (Weight(2) > 0.0)
		{
			DrawBonds(dc, sites, Weight(2));
		}
		if (Weight(3) > 0.0)
		{
			DrawDiagram(dc, Weight(3), u, stage);
		}
		double num4 = ((stage == 4) ? Ease(Clamp((u - 0.5) / 0.3)) : 0.0);
		if (num4 > 0.0)
		{
			DrawMark(dc, num4);
		}
		DrawParticles(dc, stage, u, time, num2, num4);
		if (Weight(3) > 0.0)
		{
			DrawDiagramAnnotations(dc, Weight(3), u, stage);
		}
		if (stage == 0)
		{
			DrawRockFrame(dc, u);
		}
		if (stage == 1)
		{
			DrawScan(dc, num2, u, time);
		}
		if (stage == 2)
		{
			DrawSiteLabels(dc, Weight(2));
		}
		if (stage == 4)
		{
			DrawWordmark(dc, u);
		}
		dc.Pop();
		DrawStageMarker(dc, progress, stage, u, time, width, height);
		double Weight(int s)
		{
			double result;
			if (stage != s)
			{
				result = ((stage == s + 1) ? Clamp(1.0 - u / 0.25) : 0.0);
			}
			else
			{
				result = ((s == 0) ? 1.0 : Clamp(u / 0.35));
			}
			return result;
		}
	}

	private void DrawParticles(DrawingContext dc, int stage, double u, double time, double beam, double mark)
	{
		foreach (Particle particle in _particles)
		{
			double num = 0.9;
			double num2 = particle.Size;
			Point rock;
			Point center;
			Color color;
			switch (stage)
			{
			case 0:
			{
				double num9 = Ease(Clamp(u / 0.5 - particle.Delay * 0.6));
				rock = particle.Rock;
				double x3 = rock.X * Lerp(1.5, 1.0, num9);
				rock = particle.Rock;
				center = P(x3, (rock.Y + 14.0) * Lerp(1.5, 1.0, num9) - 14.0);
				color = particle.RockColor;
				num *= num9 * (1.0 - Clamp((u - 0.4) / 0.3));
				num2 *= 0.9;
				break;
			}
			case 1:
			{
				rock = particle.Rock;
				double num6;
				if (!(rock.X < beam))
				{
					num6 = 0.0;
				}
				else
				{
					rock = particle.Rock;
					num6 = Clamp((beam - rock.X) / 80.0);
				}
				double num7 = num6;
				double num8 = Ease(Clamp((u - 0.82) / 0.18));
				rock = particle.Rock;
				double x2 = (rock.X + Math.Sin(time * 2.0 + particle.Phase) * 2.0 * num7) * (1.0 + 0.12 * num8);
				rock = particle.Rock;
				center = P(x2, (rock.Y + 14.0 - 5.0 * num7 + Math.Cos(time * 1.7 + particle.Phase) * 2.0 * num7) * (1.0 + 0.12 * num8) - 14.0);
				color = particle.ElementColor;
				num *= num7;
				num2 *= 1.15;
				break;
			}
			default:
			{
				(Point, double) tuple = Project(particle.Crystal, time);
				Point val;
				switch (stage)
				{
				default:
					val = particle.Diagram;
					break;
				case 3:
					(val, _) = tuple;
					break;
				case 2:
				{
					rock = particle.Rock;
					double x = rock.X * 1.12;
					rock = particle.Rock;
					val = P(x, (rock.Y + 14.0) * 1.12 - 19.0);
					break;
				}
				}
				Point val2 = val;
				Point val3;
				switch (stage)
				{
				default:
					val3 = particle.Logo;
					break;
				case 3:
					val3 = particle.Diagram;
					break;
				case 2:
					(val3, _) = tuple;
					break;
				}
				Point val4 = val3;
				double num3 = Ease(Clamp((u - particle.Delay * 0.5) / 0.55));
				double num4 = Math.Sin(Math.PI * num3) * particle.Curve * ((stage == 4) ? 0.6 : 1.0);
				Vector val5 = val4 - val2;
				double num5 = Math.Max(1.0, val5.Length);
				center = Lerp(val2, val4, num3) + new Vector((0.0 - val5.Y) / num5 * num4, val5.X / num5 * num4);
				switch (stage)
				{
				case 2:
					color = Mix(particle.ElementColor, particle.SiteColor, num3);
					num2 *= Lerp(1.15, 0.95 * tuple.Item2, num3);
					num *= Lerp(1.0, 0.35 + 0.65 * Clamp((tuple.Item2 - 0.8) / 0.4), num3);
					break;
				case 3:
					color = Mix(particle.SiteColor, Brand, num3);
					break;
				default:
					color = Mix(Brand, particle.FaceColor, num3);
					num2 *= Lerp(1.0, 0.9, num3);
					num *= 1.0 - mark;
					break;
				}
				break;
			}
			}
			if (num > 0.01)
			{
				dc.DrawEllipse(Brush(color, num), null, center, num2, num2);
			}
		}
	}

	private void DrawGrid(DrawingContext dc, double width, double height)
	{
		for (int i = 0; (double)i < width / 2.0; i += 32)
		{
			double alpha = 0.045 * Math.Pow(1.0 - (double)i / (width / 2.0), 2.0);
			Line(dc, P(i, (0.0 - height) / 2.0), P(i, height / 2.0), alpha);
			if (i > 0)
			{
				Line(dc, P(-i, (0.0 - height) / 2.0), P(-i, height / 2.0), alpha);
			}
		}
		for (int j = 0; (double)j < height / 2.0; j += 32)
		{
			double alpha2 = 0.045 * Math.Pow(1.0 - (double)j / (height / 2.0), 2.0);
			Line(dc, P((0.0 - width) / 2.0, j), P(width / 2.0, j), alpha2);
			if (j > 0)
			{
				Line(dc, P((0.0 - width) / 2.0, -j), P(width / 2.0, -j), alpha2);
			}
		}
	}

	private void DrawRock(DrawingContext dc, double alpha)
	{
		if (!(alpha <= 0.0))
		{
			dc.PushOpacity(Clamp(alpha));
			dc.DrawDrawing(_rockTexture);
			dc.Pop();
		}
	}

	private void DrawRockFrame(DrawingContext dc, double u)
	{
		dc.DrawEllipse(null, new Pen(Brush(Brand, 0.17), 1.0), P(0.0, -14.0), 270.0, 270.0);
		for (int i = 0; i < 72; i++)
		{
			double num = (double)i / 72.0 * (Math.PI * 2.0);
			double num2 = 270 + ((i % 6 == 0) ? 11 : 5);
			Line(dc, P(Math.Cos(num) * 270.0, -14.0 + Math.Sin(num) * 270.0), P(Math.Cos(num) * num2, -14.0 + Math.Sin(num) * num2), 0.22);
		}
		Text(dc, _copy.Headings[0], 0.0, -300.0, 11.0, 0.7, Sub);
		(string, Point, Point)[] array = new (string, Point, Point)[4]
		{
			("Qz  " + _copy.Minerals[0], P(-120.0, -60.0), P(-330.0, -150.0)),
			("Kfs  " + _copy.Minerals[1], P(150.0, -40.0), P(330.0, -140.0)),
			("Bt  " + _copy.Minerals[2], P(60.0, 110.0), P(320.0, 150.0)),
			("Pl  " + _copy.Minerals[3], P(-140.0, 60.0), P(-330.0, 140.0))
		};
		for (int j = 0; j < array.Length; j++)
		{
			(string, Point, Point) tuple = array[j];
			string item = tuple.Item1;
			Point item2 = tuple.Item2;
			Point item3 = tuple.Item3;
			double num3 = Clamp((u - 0.1 - (double)j * 0.1) / 0.2);
			Line(dc, item2, item3, 0.45 * num3);
			dc.DrawEllipse(Brush(Brand, num3), null, item2, 2.5, 2.5);
			double x = item3.X + (double)((item3.X > 0.0) ? 8 : (-8));
			double y = item3.Y;
			int align = ((!(item3.X > 0.0)) ? 1 : (-1));
			Text(dc, item, x, y, 12.5, num3, null, align, bold: false, mono: false, 205.0);
		}
	}

	private void DrawScan(DrawingContext dc, double beam, double u, double time)
	{
		LinearGradientBrush brush = new LinearGradientBrush(Colors.Transparent, Color.FromArgb(32, 47, 107, byte.MaxValue), 0.0);
		dc.DrawRectangle(brush, null, new Rect(beam - 70.0, -175.0, 73.0, 310.0));
		Line(dc, P(beam, -175.0), P(beam, 135.0), 0.95, Sites[0], 1.6);
		Text(dc, _copy.Headings[1], 0.0, -300.0, 11.0, 0.7, Sub);
		(double, double, string)[] array = new (double, double, string)[9]
		{
			(0.08, 0.35, "O"),
			(0.2, 0.25, "Na"),
			(0.27, 0.3, "Mg"),
			(0.33, 0.55, "Al"),
			(0.4, 1.0, "Si"),
			(0.58, 0.42, "K"),
			(0.66, 0.38, "Ca"),
			(0.76, 0.12, "Ti"),
			(0.9, 0.33, "Fe")
		};
		double num = Clamp((beam + 188.0) / 376.0);
		double num2 = 211.0;
		List<Point> list = new List<Point>();
		for (int i = 0; (double)i <= 300.0 * num; i++)
		{
			double num3 = (double)i / 300.0;
			double num4 = 0.03 + 0.008 * Math.Sin(num3 * 90.0 + time * 6.0);
			(double, double, string)[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				(double, double, string) tuple = array2[j];
				double item = tuple.Item1;
				double item2 = tuple.Item2;
				num4 += item2 * Math.Exp((0.0 - Math.Pow(num3 - item, 2.0)) / 9E-05);
			}
			list.Add(P(Lerp(-300.0, 300.0, num3), num2 - num4 * 60.0));
		}
		if (list.Count > 1)
		{
			dc.DrawGeometry(null, new Pen(Brush(Sites[0], 0.9), 1.3), Polyline(list));
		}
		Line(dc, P(-300.0, num2), P(300.0, num2), 0.3);
		(double, double, string)[] array3 = array;
		for (int k = 0; k < array3.Length; k++)
		{
			var (num5, num6, text) = array3[k];
			if (num5 < num)
			{
				Text(dc, text, Lerp(-300.0, 300.0, num5), num2 - num6 * 60.0 - 12.0, 10.5, 0.95, Elements[text], 0, bold: true);
			}
		}
		Text(dc, "keV", 322.0, num2, 10.0, 0.6, Sub);
		(string, string, string)[] array4 = new (string, string, string)[7]
		{
			("SiO₂", "Si", "66.2"),
			("Al₂O₃", "Al", "15.4"),
			("FeOᵀ", "Fe", "4.6"),
			("CaO", "Ca", "3.8"),
			("Na₂O", "Na", "3.9"),
			("K₂O", "K", "3.1"),
			("MgO", "Mg", "1.9")
		};
		for (int l = 0; l < array4.Length; l++)
		{
			double alpha = Clamp((num - 0.1 - (double)l * 0.1) / 0.12);
			(string, string, string) tuple3 = array4[l];
			Text(dc, tuple3.Item1, 238.0, -130 + l * 34, 13.0, alpha, Elements[tuple3.Item2], -1, bold: true);
			Text(dc, tuple3.Item3, 363.0, -130 + l * 34, 13.0, alpha, null, 1, bold: false, mono: true);
		}
		Text(dc, "wt %  ·  Σ 98.9", 300.0, 110.0, 11.0, 0.7 * Clamp((num - 0.85) / 0.15), Sub);
	}

	private void DrawBonds(DrawingContext dc, (Point Point, double Scale)[] sites, double alpha)
	{
		foreach (var bond in _bonds)
		{
			int item = bond.A;
			int item2 = bond.B;
			double num = (sites[item].Scale + sites[item2].Scale) / 2.0;
			Point item3 = sites[item].Point;
			Point item4 = sites[item2].Point;
			double alpha2 = 0.22 * alpha * Math.Clamp((num - 0.8) * 2.2, 0.15, 1.0);
			double thickness = num * 0.9;
			Line(dc, item3, item4, alpha2, null, thickness);
		}
	}

	private void DrawSiteLabels(DrawingContext dc, double a)
	{
		Text(dc, _copy.Headings[2], 0.0, -300.0, 11.0, 0.7 * a, Sub);
		for (int i = 0; i < 2; i++)
		{
			double num = -36 + i * 60;
			dc.DrawEllipse(Brush(Sites[i], a), null, P(-410.0, num), 5.0, 5.0);
			Text(dc, _copy.Sites[i], -396.0, num - 12.0, 12.5, a, null, -1, bold: true);
			Text(dc, (i == 0) ? "Si⁴⁺  Al³⁺" : "Fe²⁺  Mg²⁺  Ca²⁺", -396.0, num + 13.0, 11.5, 0.75 * a, Sub, -1);
			Text(dc, (i == 0) ? "apfu 1.000" : "apfu 0.986", 410.0, num, 12.5, 0.9 * a, null, 1, bold: false, mono: true);
		}
	}

	private void DrawDiagram(DrawingContext dc, double a, double u, int stage)
	{
		double num = ((stage == 3) ? Clamp(u / 0.45) : 1.0);
		(Point, Point)[] array = new (Point, Point)[3]
		{
			(F, M),
			(M, A),
			(A, F)
		};
		for (int i = 0; i < array.Length; i++)
		{
			double num2 = Clamp(num * 3.0 - (double)i);
			if (num2 > 0.0)
			{
				Line(dc, array[i].Item1, Lerp(array[i].Item1, array[i].Item2, num2), 0.7 * a, Ink, 1.2);
			}
		}
		for (int j = 1; j < 5; j++)
		{
			double num3 = (double)j / 5.0;
			double alpha = 0.08 * a * Clamp(num * 2.0 - 1.0);
			Line(dc, Ternary(num3, 1.0 - num3, 0.0), Ternary(num3, 0.0, 1.0 - num3), alpha);
			Line(dc, Ternary(0.0, num3, 1.0 - num3), Ternary(1.0 - num3, num3, 0.0), alpha);
			Line(dc, Ternary(1.0 - num3, 0.0, num3), Ternary(0.0, 1.0 - num3, num3), alpha);
		}
		double num4 = ((stage == 4) ? 1.0 : Clamp((u - 0.45) / 0.3));
		if (num4 > 0.0)
		{
			Point[] points = new Point[5]
			{
				Ternary(0.18, 0.72, 0.1),
				Ternary(0.25, 0.55, 0.2),
				Ternary(0.35, 0.42, 0.23),
				Ternary(0.5, 0.3, 0.2),
				Ternary(0.66, 0.2, 0.14)
			};
			Pen pen = new Pen(Brush(Rgb(217, 138, 28), 0.9 * a * num4), 1.5);
			pen.DashStyle = new DashStyle(new double[2] { 4.0, 3.3 }, 0.0);
			Pen pen2 = pen;
			dc.DrawGeometry(null, pen2, Polyline(points));
		}
		double num5 = a * Clamp(num * 2.0 - 0.6);
		Point val = A;
		double x = val.X;
		val = A;
		Text(dc, "A", x, val.Y - 20.0, 15.0, num5, null, 0, bold: true);
		val = A;
		double x2 = val.X;
		val = A;
		Text(dc, "Na₂O + K₂O", x2, val.Y - 46.0, 10.5, 0.7 * num5, Sub);
		val = F;
		double x3 = val.X - 18.0;
		val = F;
		Text(dc, "F", x3, val.Y + 14.0, 15.0, num5, null, 0, bold: true);
		val = F;
		double x4 = val.X - 18.0;
		val = F;
		Text(dc, "FeOᵀ", x4, val.Y + 42.0, 10.5, 0.7 * num5, Sub);
		val = M;
		double x5 = val.X + 18.0;
		val = M;
		Text(dc, "M", x5, val.Y + 14.0, 15.0, num5, null, 0, bold: true);
		val = M;
		double x6 = val.X + 18.0;
		val = M;
		Text(dc, "MgO", x6, val.Y + 42.0, 10.5, 0.7 * num5, Sub);
		if (stage == 3)
		{
			Text(dc, _copy.Headings[3] + "  ·  n = " + 1300.ToString("N0", _copy.Culture), 0.0, -320.0, 11.0, 0.7 * a, Sub);
		}
	}

	private void DrawDiagramAnnotations(DrawingContext dc, double alpha, double u, int stage)
	{
		double num = ((stage == 4) ? 1.0 : Clamp((u - 0.45) / 0.3));
		if (!(num <= 0.0))
		{
			Point val = Ternary(0.08, 0.62, 0.3);
			Point val2 = Ternary(0.3, 0.26, 0.44);
			Text(dc, _copy.Trends[0], val.X - 6.0, val.Y - 34.0, 11.5, 0.95 * alpha * num, Rgb(184, 110, 12), 0, bold: true, mono: false, 200.0, wrap: false, backdrop: true);
			Text(dc, _copy.Trends[1], val2.X + 16.0, val2.Y + 40.0, 11.5, 0.95 * alpha * num, Rgb(184, 110, 12), 0, bold: true, mono: false, 230.0, wrap: false, backdrop: true);
		}
	}

	private void DrawMark(DrawingContext dc, double alpha)
	{
		dc.PushOpacity(alpha);
		for (int i = 0; i < 3; i++)
		{
			dc.PushTransform(new TranslateTransform(0.0, 7.0));
			dc.DrawGeometry(Brush(Ink, 0.06), new Pen(Brush(Ink, 0.025), 8.0), _markGeometry[i]);
			dc.Pop();
			dc.DrawGeometry(new LinearGradientBrush(Faces[i], Mix(Faces[i], Colors.Black, 0.18), 45.0), null, _markGeometry[i]);
		}
		dc.Pop();
	}

	private void DrawWordmark(DrawingContext dc, double u)
	{
		double num = Ease(Clamp((u - 0.55) / 0.3));
		double num2 = Ease(Clamp((u - 0.7) / 0.3));
		double num3 = 68.45;
		Text(dc, "GeoChemistry Nexus", 0.0, num3 + (1.0 - num) * 10.0, 42.0, num, null, 0, bold: true);
		Text(dc, _copy.Features, 0.0, num3 + 62.0, 13.5, 0.9 * num2, Brand, 0, bold: false, mono: false, 1000.0, wrap: true);
		Text(dc, _copy.Tagline, 0.0, num3 + 110.0, 12.0, 0.9 * num2, Sub, 0, bold: false, mono: false, 1000.0);
	}

	private void DrawStageMarker(DrawingContext dc, double progress, int stage, double u, double time, double width, double height)
	{
		double num = height / 2.0 - 100.0;
		Line(dc, P(-236.0, num), P(236.0, num), 0.12);
		Line(dc, P(-236.0, num), P(-236.0 + 472.0 * progress, num), 0.85, null, 1.6);
		for (int i = 0; i < 5; i++)
		{
			double num2 = (i - 2) * 118;
			double num3 = ((i == stage) ? 4.5 : 3.5);
			dc.DrawEllipse((i <= stage) ? Brush(Brand, (i == stage) ? 1.0 : 0.55) : Brushes.White, (i > stage) ? new Pen(Brush(Brand, 0.4), 1.0) : null, P(num2, num), num3, num3);
			if (i == stage && !_timeline.IsComplete)
			{
				double num4 = time * 1.3 % 1.0;
				dc.DrawEllipse(null, new Pen(Brush(Brand, 0.45 * (1.0 - num4)), 1.0), P(num2, num), num3 + num4 * 9.0, num3 + num4 * 9.0);
			}
			StartupAnimation startupAnimation = this;
			DrawingContext dc2 = dc;
			string value = _copy.Stages[i];
			double x = num2;
			double y = num + 20.0;
			double alpha;
			if (i == stage)
			{
				alpha = 1.0;
			}
			else
			{
				alpha = ((i < stage) ? 0.65 : 0.5);
			}
			startupAnimation.Text(dc2, value, x, y, 13.5, alpha, (i == stage) ? Ink : Sub, 0, i == stage, mono: false, 116.0);
		}
		string text = (_timeline.IsReady ? _copy.Status((progress >= 1.0) ? StartupStatus.Completed : StartupStatus.EnteringMainWindow) : (_status ?? _copy.Status(StartupStatus.Starting)));
		text = text.Replace('\r', ' ').Replace('\n', ' ');
		Text(dc, text, -236.0, num + 56.0, 14.5, 0.95, Sub, -1, bold: false, mono: false, 390.0);
		Text(dc, $"{_timeline.LoadingProgress:0}%", 236.0, num + 56.0, 15.0, 0.95, Brand, 1, bold: true, mono: true);
		if (progress < 1.0)
		{
			double num5 = (0.0 - width) / 2.0 + 30.0;
			double num6 = (0.0 - height) / 2.0 + 30.0;
			dc.PushTransform(new TranslateTransform(num5, num6));
			dc.PushTransform(new ScaleTransform(0.13, 0.13));
			dc.PushTransform(new TranslateTransform(0.0, 78.0));
			for (int j = 0; j < 3; j++)
			{
				dc.DrawGeometry(Brush(Faces[j]), null, _markGeometry[j]);
			}
			dc.Pop();
			dc.Pop();
			dc.Pop();
			double num7 = num5 + 34.0;
			Text(dc, "GeoChemistry Nexus", num7, num6, 14.5, 0.9, null, -1, bold: true);
			double width2 = GetText("GeoChemistry Nexus", 14.5, bold: true, mono: false, 0.0, wrap: false, -1).Width;
			Text(dc, _copy.Status(StartupStatus.Starting), num7 + width2 + 20.0, num6, 11.0, 0.85, Sub, -1, bold: false, mono: false, 420.0);
		}
	}

	private static DrawingGroup LoadRockTexture()
	{
		BitmapFrame bitmapFrame = BitmapFrame.Create(new Uri("pack://application:,,,/GeoChemistryNexus;component/Data/Image/Startup/rock-thin-section.png"), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
		((Freezable)bitmapFrame).Freeze();
		DrawingGroup drawingGroup = new DrawingGroup();
		using (DrawingContext drawingContext = drawingGroup.Open())
		{
			drawingContext.DrawImage(bitmapFrame, new Rect(-174.52144, -145.0, 347.86214, 274.94951));
		}
		((Freezable)drawingGroup).Freeze();
		return drawingGroup;
	}

	private void BuildParticles()
	{
		(Point, Point, int)[] array = _mark.SelectMany((Point[] array3, int fi) => array3.Select((Point a, int i) =>
		{
			return (A: a, B: array3[(i + 1) % array3.Length], Face: fi);
		})).ToArray();
		double num = array.Sum(((Point A, Point B, int Face) e) =>
		{
			Vector val4 = e.B - e.A;
			return val4.Length;
		});
		for (int num2 = 0; num2 < 1300; num2++)
		{
			Point rock = RandomIn(_rock);
			Grain grain = _grains.MinBy((Grain g) =>
			{
				Vector val4 = g.Position - rock;
				return val4.LengthSquared;
			});
			string[] array2 = MineralElements[grain.Mineral];
			string key = array2[_random.Next(array2.Length)];
			Point3D point3D = _sites[num2 % _sites.Count];
			int num3 = ((int)(point3D.X / 58.0) + 2 + ((int)(point3D.Y / 58.0) + 2) + ((int)(point3D.Z / 58.0) + 2)) % 2;
			double num4 = _random.NextDouble();
			double num5 = _random.NextDouble();
			Point p;
			if (num4 < 0.46)
			{
				p = Ternary(0.15 + 0.75 * num5, 0.32 - 0.12 * num5 + 0.08 * Math.Sin(num5 * Math.PI), 0.57 - 0.63 * num5) + new Vector(Gauss() * 8.0, Gauss() * 8.0);
			}
			else if (num4 < 0.8)
			{
				p = Ternary(0.08 + 0.7 * num5 * num5, 0.3 + 0.45 * Math.Sin(num5 * 2.4), 0.62 - 0.6 * num5) + new Vector(Gauss() * 9.0, Gauss() * 9.0);
			}
			else
			{
				Point val = Ternary(_random.NextDouble(), _random.NextDouble(), _random.NextDouble());
				p = P(val.X * 0.85, val.Y * 0.85 + 1.5);
			}
			p = KeepInTriangle(p);
			Point logo;
			int val3;
			if (_random.NextDouble() < 0.45)
			{
				double num6 = _random.NextDouble() * num;
				int num7;
				Vector val2;
				for (num7 = 0; num7 < array.Length - 1; num7++)
				{
					double num8 = num6;
					val2 = array[num7].Item2 - array[num7].Item1;
					if (!(num8 > val2.Length))
					{
						break;
					}
					double num9 = num6;
					val2 = array[num7].Item2 - array[num7].Item1;
					num6 = num9 - val2.Length;
				}
				(Point, Point, int) tuple = array[num7];
				Point item = tuple.Item1;
				Point item2 = tuple.Item2;
				double num10 = num6;
				val2 = tuple.Item2 - tuple.Item1;
				logo = Lerp(item, item2, num10 / val2.Length);
				val3 = tuple.Item3;
			}
			else
			{
				logo = RandomIn(_mark);
				val3 = Array.FindIndex(_mark, (Point[] polygon) =>
				{
					return InPolygon(logo, polygon);
				});
			}
			_particles.Add(new Particle(rock, Mix(MineralColors[grain.Mineral], Colors.Black, 0.45), Elements[key], point3D + new Vector3D(Gauss() * 4.0, Gauss() * 4.0, Gauss() * 4.0), Sites[num3], p, logo, Faces[Math.Max(0, val3)], _random.NextDouble() * 0.38, (_random.NextDouble() - 0.5) * 110.0, 0.8 + _random.NextDouble() * 1.2, _random.NextDouble() * (Math.PI * 2.0)));
		}
	}

	private FormattedText GetText(string value, double size, bool bold, bool mono, double maxWidth, bool wrap, int align)
	{
		size *= ((size >= 40.0) ? 1.1 : 1.3);
		(string, double, bool, bool, double, bool, int) key = (value, size, bold, mono, maxWidth, wrap, align);
		if (!_texts.TryGetValue(key, out FormattedText value2))
		{
			if (_texts.Count > 220)
			{
				_texts.Clear();
			}
			value2 = new FormattedText(value, _copy.Culture, FlowDirection.LeftToRight, new Typeface(new FontFamily(mono ? ("Consolas, " + _copy.FontFamily) : _copy.FontFamily), FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal), size, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
			if (maxWidth > 0.0)
			{
				value2.MaxTextWidth = maxWidth;
				value2.MaxLineCount = ((!wrap) ? 1 : 2);
				value2.Trimming = TextTrimming.CharacterEllipsis;
				value2.TextAlignment = ((align >= 0) ? ((align > 0) ? TextAlignment.Right : TextAlignment.Center) : TextAlignment.Left);
			}
			_texts[key] = value2;
		}
		return value2;
	}

	private void Text(DrawingContext dc, string value, double x, double y, double size, double alpha = 1.0, Color? color = null, int align = 0, bool bold = false, bool mono = false, double maxWidth = 0.0, bool wrap = false, bool backdrop = false)
	{
		if (!(alpha <= 0.001) && !string.IsNullOrEmpty(value))
		{
			FormattedText text = GetText(value, size, bold, mono, maxWidth, wrap, align);
			text.SetForegroundBrush(Brush(color ?? Ink));
			double num = ((maxWidth > 0.0) ? maxWidth : text.Width);
			Point origin = P(x - ((align < 0) ? 0.0 : ((align > 0) ? num : (num / 2.0))), y - text.Height / 2.0);
			dc.PushOpacity(Clamp(alpha));
			if (backdrop)
			{
				double num2 = Math.Min(num, text.Width);
				double num3 = origin.X + ((align < 0) ? 0.0 : ((align > 0) ? (num - num2) : ((num - num2) / 2.0)));
				dc.DrawRoundedRectangle(Brush(Colors.White, 0.9), null, new Rect(num3 - 4.0, origin.Y - 2.0, num2 + 8.0, text.Height + 4.0), 3.0, 3.0);
			}
			dc.DrawText(text, origin);
			dc.Pop();
		}
	}

	private void Line(DrawingContext dc, Point a, Point b, double alpha, Color? color = null, double thickness = 1.0)
	{
		if (alpha > 0.001)
		{
			dc.DrawLine(new Pen(Brush(color ?? Brand, alpha), thickness), a, b);
		}
	}

	private SolidColorBrush Brush(Color color, double alpha = 1.0)
	{
		byte b = (byte)Math.Min(255.0, Math.Round(Clamp(alpha) * 255.0 / 8.0) * 8.0);
		uint key = (uint)((b << 24) | (color.R << 16) | (color.G << 8) | color.B);
		if (!_brushes.TryGetValue(key, out SolidColorBrush value))
		{
			if (_brushes.Count > 12000)
			{
				_brushes.Clear();
			}
			value = new SolidColorBrush(Color.FromArgb(b, color.R, color.G, color.B));
			((Freezable)value).Freeze();
			_brushes[key] = value;
		}
		return value;
	}

	private static (Point Point, double Scale) Project(Point3D v, double time)
	{
		double num = time * 0.32 + 0.6;
		double num2 = -0.42 + Math.Sin(time * 0.4) * 0.08;
		double num3 = v.X * Math.Cos(num) + v.Z * Math.Sin(num);
		double num4 = (0.0 - v.X) * Math.Sin(num) + v.Z * Math.Cos(num);
		double num5 = v.Y * Math.Cos(num2) - num4 * Math.Sin(num2);
		num4 = v.Y * Math.Sin(num2) + num4 * Math.Cos(num2);
		double num6 = 900.0 / (900.0 + num4);
		return (Point: P(num3 * num6, num5 * num6 - 20.0), Scale: num6);
	}

	private Point RandomIn(Point[][] polygons)
	{
		double a = polygons.SelectMany((Point[] p) => p).Min((Point p) => p.X);
		double b = polygons.SelectMany((Point[] p) => p).Max((Point p) => p.X);
		double a2 = polygons.SelectMany((Point[] p) => p).Min((Point p) => p.Y);
		double b2 = polygons.SelectMany((Point[] p) => p).Max((Point p) => p.Y);
		Point result;
		do
		{
			result = P(Lerp(a, b, _random.NextDouble()), Lerp(a2, b2, _random.NextDouble()));
		}
		while (!polygons.Any((Point[] p) =>
		{
			return InPolygon(result, p);
		}));
		return result;
	}

	private static bool InPolygon(Point p, Point[] polygon)
	{
		bool flag = false;
		int num = 0;
		int num2 = polygon.Length - 1;
		while (num < polygon.Length)
		{
			Point val = polygon[num];
			Point val2 = polygon[num2];
			if (val.Y > p.Y != val2.Y > p.Y && p.X < (val2.X - val.X) * (p.Y - val.Y) / (val2.Y - val.Y) + val.X)
			{
				flag = !flag;
			}
			num2 = num++;
		}
		return flag;
	}

	private static Geometry Polyline(IEnumerable<Point> points, bool closed = false)
	{
		Point[] array = points.ToArray();
		StreamGeometry streamGeometry = new StreamGeometry();
		if (array.Length != 0)
		{
			using StreamGeometryContext streamGeometryContext = streamGeometry.Open();
			streamGeometryContext.BeginFigure(array[0], closed, closed);
			streamGeometryContext.PolyLineTo(array.Skip(1).ToArray(), isStroked: true, isSmoothJoin: false);
		}
		((Freezable)streamGeometry).Freeze();
		return streamGeometry;
	}

	private static Geometry RoundedPolygon(Point[] polygon, double radius)
	{
		StreamGeometry streamGeometry = new StreamGeometry();
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			for (int i = 0; i < polygon.Length; i++)
			{
				Point val = polygon[i];
				Vector val2 = polygon[(i + polygon.Length - 1) % polygon.Length] - val;
				Vector val3 = polygon[(i + 1) % polygon.Length] - val;
				double num = Math.Min(radius, Math.Min(val2.Length, val3.Length) / 2.2);
				val2.Normalize();
				val3.Normalize();
				Point val4 = val + val2 * num;
				Point point = val + val3 * num;
				if (i == 0)
				{
					streamGeometryContext.BeginFigure(val4, isFilled: true, isClosed: true);
				}
				else
				{
					streamGeometryContext.LineTo(val4, isStroked: true, isSmoothJoin: false);
				}
				streamGeometryContext.QuadraticBezierTo(val, point, isStroked: true, isSmoothJoin: false);
			}
		}
		((Freezable)streamGeometry).Freeze();
		return streamGeometry;
	}

	private double Gauss()
	{
		return Math.Sqrt(-2.0 * Math.Log(Math.Max(double.Epsilon, _random.NextDouble()))) * Math.Cos(Math.PI * 2.0 * _random.NextDouble());
	}

	private static Point[][] Facets(double scale, double y)
	{
		return Raw.Select((Point[] p) => p.Select((Point v) =>
		{
			return P((v.X - 97.0) * scale, (v.Y - 98.0) * scale + y);
		}).ToArray()).ToArray();
	}

	private static Point Ternary(double a, double f, double m)
	{
		double num = a + f + m;
		Point val = A;
		double num2 = a * val.X;
		val = F;
		double num3 = num2 + f * val.X;
		val = M;
		double x = (num3 + m * val.X) / num;
		val = A;
		double num4 = a * val.Y;
		val = F;
		double num5 = num4 + f * val.Y;
		val = M;
		return P(x, (num5 + m * val.Y) / num);
	}

	private static Point KeepInTriangle(Point p, double margin = 0.03)
	{
		Point val = F;
		double y = val.Y;
		val = M;
		double num = y - val.Y;
		val = A;
		double x = val.X;
		val = M;
		double num2 = num * (x - val.X);
		val = M;
		double x2 = val.X;
		val = F;
		double num3 = x2 - val.X;
		val = A;
		double y2 = val.Y;
		val = M;
		double num4 = num2 + num3 * (y2 - val.Y);
		val = F;
		double y3 = val.Y;
		val = M;
		double num5 = y3 - val.Y;
		double x3 = p.X;
		val = M;
		double num6 = num5 * (x3 - val.X);
		val = M;
		double x4 = val.X;
		val = F;
		double num7 = x4 - val.X;
		double y4 = p.Y;
		val = M;
		double num8 = (num6 + num7 * (y4 - val.Y)) / num4;
		val = M;
		double y5 = val.Y;
		val = A;
		double num9 = y5 - val.Y;
		double x5 = p.X;
		val = M;
		double num10 = num9 * (x5 - val.X);
		val = A;
		double x6 = val.X;
		val = M;
		double num11 = x6 - val.X;
		double y6 = p.Y;
		val = M;
		double num12 = (num10 + num11 * (y6 - val.Y)) / num4;
		double num13 = 1.0 - num8 - num12;
		if (num8 >= margin && num12 >= margin && num13 >= margin)
		{
			return p;
		}
		return Ternary(Math.Max(num8, margin), Math.Max(num12, margin), Math.Max(num13, margin));
	}

	private static Point P(double x, double y)
	{
		return new Point(x, y);
	}

	private static Color Rgb(byte r, byte g, byte b)
	{
		return Color.FromRgb(r, g, b);
	}

	private static double Clamp(double value)
	{
		return Math.Clamp(value, 0.0, 1.0);
	}

	private static double Ease(double value)
	{
		return (value < 0.5) ? (4.0 * value * value * value) : (1.0 - Math.Pow(-2.0 * value + 2.0, 3.0) / 2.0);
	}

	private static double Lerp(double a, double b, double t)
	{
		return a + (b - a) * t;
	}

	private static Point Lerp(Point a, Point b, double t)
	{
		return P(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t));
	}

	private static Color Mix(Color a, Color b, double t)
	{
		return Rgb((byte)Lerp((double)(int)a.R, (double)(int)b.R, t), (byte)Lerp((double)(int)a.G, (double)(int)b.G, t), (byte)Lerp((double)(int)a.B, (double)(int)b.B, t));
	}

	static StartupAnimation()
	{
	}
}
