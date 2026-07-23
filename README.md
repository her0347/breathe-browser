# breathe browser

__________________________________________________________________________________________________________________________________________________________________________________________________________________

breathe browser is a browser that uses Cefsharp to render web content and EasyTabs for a tabbing system. The tabs are reminiscient of legacy Chrome trapezoid tabs.

breathe browser is developed in C# Winforms (.Net Framework).

___________________________________________________________________________________________________________________________________________________________________________________________________________________

breathe browser collects no personal data, however some may be collected by packages, so it is recommended that you are comfortable with the data practices of Cefsharp and EasyTabs.


# about:___ 

about:credits

about:breathe
____________________________________________________________________________________________________________________________________________________________________________________________________________________

# Upcoming features (in order of priority / most likely to least likely)

- History
- Bookmarks
- Extension support
- Password manager


# In development

- Allow downloads
- Firefox-style download UI
- More options for right-click menu

__________________________________________________________________________________________________________________

# Known Issues

There are a few known issues that I am either working on, unable to fix, or not under current development. If you find an error not listed here, please let me know so I can add it to here and fix it.

##Embedded content from sites like Youtube##
I found an issue when doing a usability test. I found that Songsterr could not play the original version of a song from Youtube, whilst it could using a Firefox based browser (Floorp) This is likely an issue with Cefsharp. As it doesn't seem to drastically affect anything at the moment, this is a fix that is fairly low priority.

##Downloads button##
The downloads button to the right of the URL bar is currently a placeholder. It should work if your downloads folder is in the default location (something like C:/user/user/downloads), but if it is in a different location (i.e. a D drive) if will not open to the correct downloads folder.
