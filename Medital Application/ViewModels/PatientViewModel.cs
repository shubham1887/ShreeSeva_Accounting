using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class PatientViewModel : ObservableObject
{
    private readonly IPatientRepository _patientRepo;
    private readonly IDoctorRepository _doctorRepo;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Patient? _selectedPatient;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<Patient> Patients { get; } = new();
    public ObservableCollection<Doctor> Doctors { get; } = new();

    public PatientViewModel(IPatientRepository patientRepo, IDoctorRepository doctorRepo)
    {
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var patients = await _patientRepo.SearchAsync(string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
            Patients.Clear();
            foreach (var p in patients) Patients.Add(p);
            var doctors = await _doctorRepo.GetAllAsync();
            Doctors.Clear();
            foreach (var d in doctors) Doctors.Add(d);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedPatient == null) return;
        if (SelectedPatient.Id == 0)
            await _patientRepo.CreateAsync(SelectedPatient);
        else
            await _patientRepo.UpdateAsync(SelectedPatient);
        StatusMessage = "Patient saved.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(Patient patient)
    {
        await _patientRepo.DeleteAsync(patient.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private void NewPatient() => SelectedPatient = new Patient { PatientCode = $"PAT{DateTime.Now:mmss}" };
}
