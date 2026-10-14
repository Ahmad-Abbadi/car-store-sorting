"""Reference-board CORE colors and solid-plastic accessory colors."""

CORE_COLORS={
    "CC-01":"#E84C45", "CC-02":"#70B94A", "CC-03":"#EDAA27", "CC-04":"#287DCA", "CC-05":"#8B4ACB",
    "TC-01":"#1765D7", "TC-02":"#FF831C", "TC-03":"#E5483F", "TC-04":"#7D8B35", "TC-05":"#803FC4",
    "RR-01":"#D93B37", "RR-02":"#4EAE9B", "RR-03":"#3D90D2", "RR-04":"#25272E", "RR-05":"#7742AF",
    "DP-01":"#596F42", "DP-02":"#E9762A", "DP-03":"#D1A14C", "DP-04":"#3B96A4", "DP-05":"#D94E3B",
    "BW-01":"#F17822", "BW-02":"#3456C1", "BW-03":"#D9493F", "BW-04":"#E4B52F", "BW-05":"#8D45B5",
    "RC-01":"#E83B3D", "RC-02":"#F4F5F3", "RC-03":"#CF3F3B", "RC-04":"#E0AB35", "RC-05":"#2878C7",
    "WY-01":"#E5B226", "WY-02":"#2C9EA6", "WY-03":"#EB8137", "WY-04":"#29979D", "WY-05":"#E3AA29",
    "SS-01":"#F49AB2", "SS-02":"#F1B748", "SS-03":"#DA873E", "SS-04":"#F0F0EB", "SS-05":"#ED91AE",
    "FF-01":"#E9F0F9", "FF-02":"#D8C9ED", "FF-03":"#EFC735", "FF-04":"#E17E36", "FF-05":"#D8494B",
    "CR-01":"#277BD6", "CR-02":"#91C451", "CR-03":"#D95D40", "CR-04":"#78BF49", "CR-05":"#8E4BC3",
}

# Food props and a few thematic details need their own material. Other
# accessories take the corresponding body paint instead of one family color.
ACCESSORY_COLORS={
    **CORE_COLORS,
    "SS-01":"#F4E9D5", "SS-02":"#DD8D3D", "SS-03":"#DA9B4B",
    "SS-04":"#F7F7F0", "SS-05":"#EC81AA", "FF-01":"#28C7E8",
    "RC-02":"#F4F5F3", "RR-04":"#34353A",
}
