// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;

namespace StudyDiary.Data.Tests;

public class RecoveryCopyShould
{
    [Theory]
    [InlineData("2026-10-06_00-30-00", true)]
    [InlineData("2026-10-06_00-30-00_2", true)]
    [InlineData("2026-10-06_00-30-00_12", true)]
    [InlineData("my notes", false)]
    [InlineData("2026-10-06_00-30-00_x", false)]
    [InlineData("2026-10-06_00-30-00_1", false)]
    [InlineData("2026-10-06_00-30-00_", false)]
    [InlineData("2026-13-06_00-30-00", false)]
    public void RecogniseOnlyFolderNamesOfItsOwn(string name, bool isOurs) =>
        Assert.Equal(isOurs, RecoveryCopy.FromFolder(Path.Combine("recovery", name)) is not null);

    [Fact]
    public void ReadWhenItWasTakenFromItsName()
    {
        var copy = RecoveryCopy.FromFolder(Path.Combine("recovery", "2026-10-06_14-30-05_3"));

        Assert.NotNull(copy);
        Assert.Equal(new DateTime(2026, 10, 6, 14, 30, 5), copy.TakenAt);
    }
}
