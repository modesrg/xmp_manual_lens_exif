# xmp_manual_lens_exif

Lenxif is a library based around XMPCore that allows to include Exif data from manual lenses using the sidecar XMP files created by Adobe Lightroom.
It works for entire folders or individual images and it can be done manually (where the implementing project inserts the lens, focal length and aperture) or, 
and here's the neat part, automatically. 

When using the auto function Lenxif will try to extract the required data from the tags and apply them to the Exif data.
