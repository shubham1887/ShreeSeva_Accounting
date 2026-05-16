using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class DoctorViewModel : ObservableObject
{
    private readonly IDoctorRepository _doctorRepo;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Doctor? _selectedDoctor;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<Doctor> Doctors { get; } = new();

    public DoctorViewModel(IDoctorRepository doctorRepo) => _doctorRepo = doctorRepo;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var doctors = await _doctorRepo.SearchAsync(string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
            Doctors.Clear();
            foreach (var d in doctors) Doctors.Add(d);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedDoctor == null) return;
        if (SelectedDoctor.Id == 0) await _doctorRepo.CreateAsync(SelectedDoctor);
        else await _doctorRepo.UpdateAsync(SelectedDoctor);
        StatusMessage = "Doctor saved.";
        await LoadAsync();
    }

    [RelayCommand]
    private void NewDoctor() => SelectedDoctor = new Doctor { DoctorCode = $"DOC{DateTime.Now:mmss}" };
}
