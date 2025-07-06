# AnimationSet: Sayaka
VAR IsBeginner = false
[STORY_INTRO_1]# Position: Bottom # Speaker: Sayaka # Animation: Sayaka/Default
[STORY_INTRO_2]# Animation: Sayaka/Happy
[STORY_INTRO_3]# Animation: Sayaka/Default
[STORY_INTRO_4]# Highlight: FreePlayCard # WaitForHighlightOnClick
[N/A] # OverlayOpacity: 0/0.4/0 # Duration: 0.4
[STORY_INTRO_5]# OverlayOpacity: 0.7/0.4/0.4
[STORY_INTRO_6]# Animation: null # Highlight: @LevelSelectionScreen/HighlightedLevelCard
[STORY_INTRO_7]# Highlight: LevelSelectionScreen/Back # WaitForHighlightOnClick # Animation: Sayaka/Default
[N/A] # Duration: 0.8
{ IsBeginner == true:
    -> community_beginner
- else:
    -> community
}

=== community_beginner ===
[STORY_INTRO_8]# Animation: Sayaka/Surprised
[STORY_INTRO_9]# Animation: Sayaka/Happy
-> cont

=== community ===
[STORY_INTRO_10]# Animation: Sayaka/Happy
-> cont

=== cont ===
[STORY_INTRO_11]# Position: Top # Highlight: CommunityCard
[STORY_INTRO_12]# Highlight: CommunityCard
[STORY_INTRO_13]
[STORY_INTRO_14]# Highlight: EventsCard
[STORY_INTRO_15]# Highlight: TrainingCard
{ IsBeginner == false:
    [STORY_INTRO_16]# Highlight: TrainingCard
}
[STORY_INTRO_17]# Highlight: TiersCard
[STORY_INTRO_18]# Position: Bottom # Animation: Sayaka/Happy
-> DONE
