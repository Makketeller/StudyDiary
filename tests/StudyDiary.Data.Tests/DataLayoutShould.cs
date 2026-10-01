// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;

namespace StudyDiary.Data.Tests;

public class DataLayoutShould
{
    [Fact]
    public void KeepTheDefaultProfileFolderNamesFixed()
    {
        var root = Path.Combine("machine", "data");

        var folder = DataLayout.DefaultProfileFolder(root);

        Assert.Equal(
            Path.Combine(root, "studydiary", "profiles", "default"),
            folder);
    }
}
