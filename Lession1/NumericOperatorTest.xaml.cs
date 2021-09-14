using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Lession1.Annotations;
using Lession1.Numeric;

namespace Lession1
{
  public partial class NumericOperatorTest : Page
  {
    private Holder _holder;

    public NumericOperatorTest()
    {
      InitializeComponent();
    }

    private void _onLoaded(object sender, RoutedEventArgs e)
    {
      _holder = (Holder)this.FindResource("Holder");
      _holder.Next();
      _holder.End += _onEnd;
    }

    private void _onAssert(object sender, RoutedEventArgs e)
    {
      _holder.Assert();
    }

    private void _onNext(object sender, RoutedEventArgs e)
    {
      _holder.Next();
    }

    private void _onEnd()
    {
      var totalDialog = new TotalDialog(_holder.TotalContext.CurrentScore, Holder.Max * Holder.TrueScore);
      totalDialog.ShowDialog();
      var ns = this.NavigationService;
      ns?.Navigate(new Uri("Welcome.xaml", UriKind.Relative));
    }
  }

  public sealed class Holder : INotifyPropertyChanged
  {
    public const int Max = 5;
    public const int TrueScore = 1;
    private const int MaxTime = 5;
    private readonly Numeric.Generator _generator = new();
    private readonly HolderWithMax _holder;
    public QuestionContext QuestionContext { get; }
    public QuestionStateContext QuestionStateContext { get; }
    public ButtonContext ButtonContext { get; } = new();
    public TotalContext TotalContext { get; }
    public TimerContextHolder TimerContextHolder { get; private set; }

    public delegate void EndHandler();

    public event EndHandler End;

    public Holder()
    {
      _holder = new Numeric.HolderWithMax(Max);
      QuestionContext = new QuestionContext(_holder);
      QuestionContext.PropertyChanged += (sender, args) =>
      {
        if (args.PropertyName == "State")
        {
          var state = QuestionContext.State;
          if (state == QuestionContext.QuestionState.Waiting)
            ButtonContext.ChangeVisibility(ButtonContext.Show.Assertion);
          else if (state == QuestionContext.QuestionState.End)
          {
            ButtonContext.ChangeVisibility(ButtonContext.Show.Nothing);
            TotalContext.CurrentScore = _holder.Score(TrueScore);
            End?.Invoke();
          }
          else
            ButtonContext.ChangeVisibility(ButtonContext.Show.Next);

          QuestionStateContext.State = state;
        }
      };
      QuestionStateContext = new QuestionStateContext(QuestionContext.State);
      TotalContext = new TotalContext(Max);
      TimerContextHolder = new TimerContextHolder(MaxTime);
      TimerContextHolder.Next += Skip;
      Skip();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Next()
    {
      QuestionContext.Next(_generator.Generate());
      TotalContext.CurrentCount = _holder.Count + 1;
      TimerContextHolder.Refresh();
    }

    public void Assert()
    {
      QuestionContext.Assert();
      TotalContext.CurrentScore = _holder.Score(TrueScore);
    }

    public void Skip()
    {
      QuestionContext.Skip(_generator.Generate());
      TotalContext.CurrentCount = _holder.Count + 1;
      TimerContextHolder.Refresh();
    }
  }

  public sealed class QuestionContext : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public enum QuestionState
    {
      Waiting,
      T,
      F,
      End
    }

    private readonly Numeric.HolderWithMax _holderWithMax;
    private Numeric.Expression _currentExpression;
    private string _currentAttempt = "";
    private QuestionState _state = QuestionState.Waiting;

    public QuestionContext(Numeric.HolderWithMax holderWithMax) => this._holderWithMax = holderWithMax;

    public string CurrentText => _currentExpression?.Text;

    public string CurrentAttempt
    {
      get => _currentAttempt;
      set
      {
        if (value == _currentAttempt) return;
        _currentAttempt = value;
        OnPropertyChanged();
      }
    }

    public QuestionState State
    {
      get => _state;
      private set
      {
        if (value == _state) return;
        _state = value;
        OnPropertyChanged();
      }
    }

    public void Assert()
    {
      if (State != QuestionState.Waiting) return;
      var parsed = int.TryParse(_currentAttempt, out var attempt);
      if (!parsed) attempt = Int32.MinValue;
      var (assertion, fulled) = _holderWithMax.AddWithMax(_currentExpression, attempt);
      State = fulled ? QuestionState.End : (assertion ? QuestionState.T : QuestionState.F);
    }

    public bool Next(Numeric.Expression expression)
    {
      if (State is QuestionState.Waiting or QuestionState.End) return false;
      _currentExpression = expression;
      State = QuestionState.Waiting;
      OnPropertyChanged(nameof(CurrentText));
      return true;
    }

    public bool Skip(Numeric.Expression expression)
    {
      if (State == QuestionState.Waiting && _currentExpression != null) Assert();
      if (State is QuestionState.End) return false;
      _currentExpression = expression;
      State = QuestionState.Waiting;
      OnPropertyChanged(nameof(CurrentText));
      return true;
    }
  }

  public sealed class QuestionStateContext : INotifyPropertyChanged
  {
    private static readonly Dictionary<QuestionContext.QuestionState, string> Map = new();

    private QuestionContext.QuestionState _state;

    public QuestionContext.QuestionState State
    {
      set
      {
        _state = value;
        OnPropertyChanged(nameof(StateText));
      }
    }

    public string StateText => Map.ContainsKey(_state) ? Map[_state] : "";

    static QuestionStateContext()
    {
      Map.Add(QuestionContext.QuestionState.T, "Yes! You are right!");
      Map.Add(QuestionContext.QuestionState.F, "No...You're mistake.");
    }

    public QuestionStateContext(QuestionContext.QuestionState state)
    {
      State = state;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }

  public sealed class TotalContext : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly int _max;

    private int _currentCount;

    public int CurrentCount
    {
      get => throw new NotImplementedException();
      set
      {
        if (value == _currentCount) return;
        _currentCount = value;
        OnPropertyChanged(nameof(CurrentCountText));
      }
    }

    public string CurrentCountText => $"Question {_currentCount}/{_max}";

    private int _currentScore;

    public int CurrentScore
    {
      get => _currentScore;
      set
      {
        if (value == _currentScore) return;
        _currentScore = value;
        OnPropertyChanged(nameof(CurrentScoreText));
      }
    }

    public string CurrentScoreText => $"Score: {_currentScore}";

    public TotalContext(int max) => _max = max;
  }

  public sealed class ButtonContext : INotifyPropertyChanged
  {
    public enum Show
    {
      Assertion,
      Next,
      Nothing
    }

    private Show _t;

    public void ChangeVisibility(Show item)
    {
      _TChanged(item);
    }

    public Visibility AssertionButtonVisibility => _t == Show.Assertion ? Visibility.Visible : Visibility.Hidden;
    public Visibility NextButtonVisibility => _t == Show.Next ? Visibility.Visible : Visibility.Hidden;
    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void _TChanged(Show value)
    {
      _t = value;
      OnPropertyChanged(nameof(AssertionButtonVisibility));
      OnPropertyChanged(nameof(NextButtonVisibility));
    }
  }

  public sealed class TimerContextHolder : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public delegate void NextHandler();

    public event NextHandler Next;

    private TimerContext _current;

    public int CurrentTime => _current.CurrentTime;
    public int TotalTime { get; }

    public TimerContextHolder(int totalTime)
    {
      TotalTime = totalTime;
      Refresh();
    }

    public void Refresh()
    {
      _current?.Dispose();
      _current = new TimerContext(TotalTime, new DispatcherTimer());
      _current.PropertyChanged += (sender, args) =>
      {
        if (args.PropertyName == nameof(CurrentTime))
        {
          OnPropertyChanged(nameof(CurrentTime));
        }
      };
      if (Next != null) _current.Next += Next.Invoke;
    }
  }

  public sealed class TimerContext : INotifyPropertyChanged, IDisposable
  {
    public delegate void NextHandler();

    public event PropertyChangedEventHandler PropertyChanged;
    public event NextHandler Next;


    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private DispatcherTimer _timer;
    private int _currentTime;
    private int _totalTime;

    public int TotalTime
    {
      get => _totalTime;
      set
      {
        if (value == _totalTime) return;
        _totalTime = value;
        OnPropertyChanged();
      }
    }

    public int CurrentTime
    {
      get => _currentTime;
      set
      {
        if (value == _currentTime) return;
        _currentTime = value;
        OnPropertyChanged();
      }
    }

    public TimerContext(int totalTime, DispatcherTimer timer)
    {
      _timer = timer;
      _timer.Tick += (sender, args) =>
      {
        CurrentTime--;
        if (CurrentTime <= 0)
        {
          timer.Stop();
          CurrentTime = totalTime;
          Next?.Invoke();
        }
      };
      _timer.Interval = new TimeSpan(0, 0, 1);

      _totalTime = totalTime;
      _currentTime = _totalTime;
      _timer.Start();
    }

    private void ReleaseUnmanagedResources()
    {
      _timer.Stop();
    }

    public void Dispose()
    {
      ReleaseUnmanagedResources();
      GC.SuppressFinalize(this);
    }

    ~TimerContext()
    {
      ReleaseUnmanagedResources();
    }
  }

  namespace Numeric
  {
    public class Generator
    {
      private readonly Random _rand = new Random();

      public Expression Generate()
      {
        int a = _rand.Next(0, 10);
        int b = _rand.Next(1, 10);
        Operator op = (Operator)_rand.Next(0, 4);
        return new Expression(a, b, op);
      }
    }

    public class Holder
    {
      private readonly Stack<(Expression, int)> _trueStack = new();
      private readonly Stack<(Expression, int)> _falseStack = new();

      public virtual bool Add(Expression expression, int result)
      {
        var valid = expression.Assert(result);
        if (valid) _trueStack.Push((expression, result));
        else _falseStack.Push((expression, result));
        return valid;
      }

      public int Score(int trueScore, int falseScore = 0) =>
        _trueStack.Count * trueScore + _falseStack.Count * falseScore;
    }

    public class HolderWithMax : Holder
    {
      private readonly int _max;
      public int Count { get; private set; }

      public HolderWithMax(int max) => _max = max;

      public (bool, bool) AddWithMax(Expression expression, int result)
      {
        var af = Count >= _max;
        if (af) return (false, false);
        var r = base.Add(expression, result);
        Count++;
        var f = Count >= _max;
        return (r, f);
      }

      public sealed override bool Add(Expression expression, int result)
      {
        throw new NotImplementedException();
      }
    }

    public class Expression
    {
      private readonly int _groundTruth;
      private readonly int[] _nums;

      public Expression(int a, int b, Operator op)
      {
        switch (op)
        {
          case Operator.Add:
            _groundTruth = a + b;
            break;
          case Operator.Sub:
            _groundTruth = a - b;
            break;
          case Operator.Mul:
            _groundTruth = a * b;
            break;
          case Operator.DivideBy:
            _groundTruth = a / b;
            break;
          default:
            throw new EvaluateException();
        }

        _nums = new[] { a, b, (int)op };
      }

      public string Text => $"{_nums[0]} {OperatorHelper.StringMapper[(Operator)_nums[2]]} {_nums[1]} =";

      public bool Assert(int num) => num == _groundTruth;
    }

    public enum Operator
    {
      Add,
      Sub,
      Mul,
      DivideBy
    }

    public static class OperatorHelper
    {
      public static Dictionary<Operator, string> StringMapper = new();

      static OperatorHelper()
      {
        StringMapper.Add(Operator.Add, "+");
        StringMapper.Add(Operator.Sub, "-");
        StringMapper.Add(Operator.Mul, "*");
        StringMapper.Add(Operator.DivideBy, "/");
      }
    }
  }
}