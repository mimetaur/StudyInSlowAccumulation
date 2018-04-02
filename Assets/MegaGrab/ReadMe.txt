Mega Grab is a script that allows you to grab upscaled and anti aliased screenshots. If you ever need to do real prints of your game for posters or banners, or need to grab an even more anti aliased image then this script will do that. You say how many times you want the grab to be bigger than the actual rendered size and how many extra levels of anti aliasing you require and when you grab Mega Grab will automatically render, upscale and antialias the camera to a file.

Please note if you are upscaling a lot and with many AA samples it could take a few minutes for a grab to happen.

The params in the script are:
SrcCamera		- camera to use for screenshot
GrabKey			- Key to grab the screenshot
ResUpscale		- How much to increase the screen shot res by
Blur			- Pixel oversampling. Use to slightly blur the AA samples if you still notice artifacts.
AASamples		- Anti aliasing samples.
FilterMode		- Filter mode. Can be used to turn of filtering if upscaling.
UseJitter		- Use random jitter for AA sampling. Mega Grab will use a grid array for sampling but this can lead
				  to artifacts on noisy images to try setting this for a random array
SaveName		- Base name for grabs
Format			- format string for date time info
Path			- Path to directory to save files in, must have a trailing '/' if left as "" then will save in the
				  game dir.
UseDOF			- Turn on DOF grab
totalSegments	- How many samples to take around the dof camera circle
sampleRadius	- radius of the DOF camera circle
		
UseDOF			- Use Dof grab
focalDistance	- DOF focal distance
totalSegments	- How many DOF samples
sampleRadius	- Amount of DOF effect
CalcFromSize	- Let grab calc res from dpi and Width(in inches)
Dpi				- Number of Dots per inch required
Width			- Final physical size of grab using Dpi
NumberOfGrabs	- Read only of how many grabs will happen
EstimatedTime	- Guide to show how long a grab will take in Seconds
GrabWidthWillBe	- Width of final image
GrabHeightWillBe- Height of final Image
UseCoroutine	- If you get a ReadPixels error on newer version of Unity turn this on			  
Upload Grabs	- If jpg format then you can upload the file instead of saving it locally
Url				- The url to the php code below on your server

Updates:
Changed system to grab to ram and not vram allowing for any size grabs just limited by memory.
Output is TGA for the moment until I get my Png encoder working properly.
Added early DOF support, needs testing.
I have added auto calc of the scale value if you provide Dpi and with values, also added values to show the output image size and how long the grab will take in seconds;

Plans:
Add uploader - Done
			  
Any problems or suggestions please email me at chris@west-racing.com

Chris West
Version 1.31
Added error checking at file write time so if no file is written a reason why is given in console Window. This has been added as some Mac systems have write protect on some folders and grabs were not being saved, you will now be able to see why a grab has not been saved.

Version 1.30
MegaGrab fully compatible with Unity 2017
Added 'Grab From Start' option to tell MegaGrab to either do a screen or sequence grab from the very first frame when playing.

Version 1.29
MegaGrab made fully Unity 5.5 compatible.

Version 1.28
Grab paths now work correctly if using relative paths.
Path correctly handles leading and trailing '\'

Version 1.27
Added option to not include the Date and Time etc info in the sequence grab file names.

Version 1.26
Added DoScreenGrab() method to MegaGrab which can be called from GUI methods etc to do a grab instead of using a Key Press.
Added CancelSeqGrab() method to MegaGrab which can be called to cancel a seq grab instead of using a key press.

Version 1.25
Fully Unity 5 compatible

Version 1.24
Added support for doing alpha grabs when using TGA format.

Version 1.23
Sequence Grab now uses the format string
Sequence grab frame number now has leading zeros for easier sorting

Version 1.22
Grab size label in inspector will now show correct size based on game view size.

Version 1.21
Two warnings removed from code.

Version 1.19
Forced garbabge collection at end of Grab to help memory use on multiple grabs
Inspector now updates the grab size and estimated time on param changes.
System default is no to use Co Routine so in lin ewith Unity 4.x use

Version 1.18
Added an option to allow you to grab sequences which can then be made into a video with offline software.
Added custom inspector for MegaGrab to make it a bit tidier.

Version 1.17
Changed near and far camera values to nearClipPlane and farClipPlane

Version 1.15
Fixed multiple Application. error.
Fixed #ifdef used instead of #if error.

Version 1.14
Added support to upload the grabs to the internet

// Php Code, upload this to your server
<?php
    //check if something its being sent to this script
    if ($_POST)
    {
        //check if theres a field called action in the sent data
        if ( isset ($_POST['action']) )
        {
            //if it indeed theres an field called action. check if its value its level upload
            if($_POST['action'] === 'level upload')
            {
                if(!isset($_FILES) && isset($HTTP_POST_FILES))
                {
                    $_FILES = $HTTP_POST_FILES;
                }
                   
                if ($_FILES['file']['error'] === UPLOAD_ERR_OK)
                {
                    //check if the file has a name, in this script it has to have a name to be stored, the file name is sent by unity
                    if ($_FILES['file']['name'] !== "")
                    {
                        //this checks the file mime type, to filter the kind of files you want to accept, this script is configured to accept only jpg files
                        if ($_FILES['file']['type'] === 'images/jpg')
                        {
                            $uploadfile =  $_FILES['file']['name'];
                            $newimage = "images/" . $uploadfile;
                            move_uploaded_file($_FILES['file']['tmp_name'], $newimage);              
                        }
                    }
                }
            }
        }   
    }
?>
