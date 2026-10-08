// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;
using System.Text.Json;

namespace StudyDiary.Data.Tests;

public class RefusedFileExceptionShould
{
    [Fact]
    public void CountTheLineFromOneInItsDamageDetail()
    {
        var cause = new JsonException("broken", "$.dayLogs", lineNumber: 2, bytePositionInLine: 13);

        var detail = new RefusedFileException("payload.json", cause).ToDamageDetail();

        Assert.Equal(new DamageDetail("payload.json", 3, "$.dayLogs", "broken"), detail);
    }


    [Fact]
    public void GiveNoLineInItsDamageDetailWhenTheCauseHasNone()
    {
        var cause = new JsonException(
            "broken", "$.entries[0]", lineNumber: null, bytePositionInLine: null);

        var detail = new RefusedFileException("payload.json", cause).ToDamageDetail();

        Assert.Null(detail.Line);
    }

}
