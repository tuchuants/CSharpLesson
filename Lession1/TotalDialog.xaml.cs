using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Lession1.Annotations;

namespace Lession1
{
  public partial class TotalDialog : Window, INotifyPropertyChanged
  {
    private int _score;
    private int _maxScore;

    public int Score
    {
      get => _score;
      set
      {
        if (value == _score) return;
        _score = value;
        OnPropertyChanged();
      }
    }

    public int MaxScore
    {
      get => _maxScore;
      set
      {
        if (value == _maxScore) return;
        _maxScore = value;
        OnPropertyChanged();
      }
    }

    public TotalDialog(int score, int maxScore)
    {
      InitializeComponent();
      Score = score;
      MaxScore = maxScore;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void _close(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}