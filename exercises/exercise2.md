---
layout: default
title: Exercise2
permalink: /exercise2/
---

[Exercise 1]({{ site.baseurl }}{% link exercises/exercise1.md %})\
[Exercise 2]({{ site.baseurl }}{% link exercises/exercise2.md %})\
<!--[Exercise 3]({{ site.baseurl }}{% link exercises/exercise3.md %})\-->
[Lecture Slides]({{ site.baseurl }}{% link ebd_lectureslides.md %})\
[Software]({{ site.baseurl }}{% link software.md %})

#  Analyzing behavioral differences across hospital typologies (submission: 22.10)
The goal of this exercise is to analyze how different hospital typologies differ with respect to behavioral differences, such as wayfinding, the efficiency of nursing care and quality of care.
**The analysis will be done using the three measures presented in class, and using the custom scripts built for the course (using grasshopper, see software page for instructions). Follow the steps in the video tutorial below to complete the analysis (please visit the software page and make sure you installed all the necessary software).**

**A. Choose a minimum of 2 hospital layouts, model it in 2D, and compare it across the following measures:**

* **Circulation complexity: Inter-Connected Density (ICD)**, (O’Neill, 1991): ICD is the mean number of potential paths from any decision point within the floor plan. 
> Lower  ICD is correlated with better wayfinding 

* **Nursing routine efficiency: Yale Traffic Index (YTI)** (Thompson and Goldin, 1975)
The relative trip frequencies of selected traffic links multiplied by the distance between links. 
> Lower YTI is associated with more efficient nursing routines 

* **Quality of Care:  Spatial Communication Index (SCI)** (Pachilova and Sailer, 2020): SCI is the accumulated visibility for key traffic links in a ward divided by the number of beds. 
> Higher SCI is correlated with better quality of care 

**B. For each typology:**
* Export an image (See step 6 in the video tutorial) showing the analysis for each measure. 
* Drag and drop these files to the Miro board (link is sent by email) and compare the two typologies across the three measures. 
* Write a figure caption summarising your comparison. Your caption should be simple, and critical. What is the reason for the observed differences? Is it the configuration? The location of critical functions (e.g., the nurse station)? The size of the layout? 
![An example of the comparison matrix comparing both typologies across the three measures](/assets/images/ExampleMatrix.JPG)

* Each student is required to present their results in class on the 22.10  (max 5 minutes per student) 

**C. Submission files**
* Upload the images and the Rhino file (.3dm file, name as StudentFirstNameLastName.3dm) to polybox to this [link](https://polybox.ethz.ch/index.php/s/pVmCbbMoBLpgyP2)

# Video Tutorial 
## 1. Preprocessing the Layout

<iframe width="560" height="315" src="https://www.youtube.com/embed/aBzpHc8Mf4U" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * *

## 2. Opening the Script and Computing the Medial Axis

<iframe width="560" height="315" src="https://www.youtube.com/embed/qEaVwEMZeyY" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * *

## 3. Building the Graph

<iframe width="560" height="315" src="https://www.youtube.com/embed/hQt4_EK0nKw" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * *

## 4. Computing Measures

<iframe width="560" height="315" src="https://www.youtube.com/embed/cmsKklp2qEw" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * *

## 5. Visualizing Measures

<iframe width="560" height="315" src="https://www.youtube.com/embed/g0aWGNtUkQg" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * * 

## 6. Rendering a Visualization to an Image File

<iframe width="560" height="315" src="https://www.youtube.com/embed/5IcUh2u1Ox0" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

* * *
