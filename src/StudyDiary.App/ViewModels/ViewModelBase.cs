// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudyDiary.App.ViewModels;

/// <summary>
/// Tells the view when a property changes, written by hand rather than
/// taken from an MVVM library (ARCHITECTURE).
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Stores <paramref name="value"/> and raises <see cref="PropertyChanged"/>,
    /// unless it equals what is already there. True when it changed.
    /// </summary>
    protected bool SetField<T>(
        ref T backingField, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
            return false;

        backingField = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
