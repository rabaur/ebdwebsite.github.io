---
layout: default
title: Exercise2
permalink: /exercise2/
---

[Exercise 1]({{ site.baseurl }}{% link exercise1.md %})\
[Exercise 2]({{ site.baseurl }}{% link exercise2.md %})\
[Lecture Slides]({{ site.baseurl }}{% link ebd_lectureslides.md %})\
[Software]({{ site.baseurl }}{% link software.md %})

#  Analyzing behavioral differences across hospital typologies (submission: 22.10)
The goal of this exercise is to analyze how different hospital typologies differ with respect to behavioral differences, such as wayfinding, the efficiency of nursing care and quality of care.
The analysis will be done using the three measures presented in class, and using the custom scripts built for the course (using grasshopper, see software page for instructions). 

Choose a minimum of 2 hospital layouts and compare it across the following measures:
* Circulation complexity: Inter-Connected Density (ICD), (O’Neill, 1991): ICD is the mean number of potential paths from any decision point within the floor plan. Lower  ICD is correlated with better wayfinding 
•Nursing routine efficiency: Yale Traffic Index (Thompson and Goldin, 1975)
The relative trip frequencies of selected traffic links multiplied by the distance between links. Lower YTI is associated with more efficient nursing routines 
•Quality of Care:  Spatial Communication Index (Pachilova and Sailer, 2020): SCI is the accumulated visibility for key traffic links in a ward divided by the number of beds. A higher SCI is correlated with better quality of care 

For each typology:
*  export an image (See step 6 in the video tutorial) showing the analysis for each measure. 
* Drag and drop these files to the Miro board (link is sent by email) and compare the two typologies across the three measures. 
* Write a figure caption summarising your comparison. Your caption should be simple, and critical. What is the reason for the observed differences? Is it the configuration? The location of critical functions (e.g., the nurse station)? The size of the layout? 
* Each student is required to present their results in class on the 22.10  (max 5 minutes per student) 
<!--

## 1. Model the typology
* 2D orthographic floorplan
* Possible resources: List of typologies but also choose from other databases.
* Do & Dont's (with images)

## 2. Generating the Measures

> IMPORTANT: In use of these tools ... -> polybox.ch

* Brief overview on Grasshopper:

### 2.1 Set the Inputs

### 2.2 Output 1: Medial Axis
* Short description (what is it)? This should include explanations about the types of segments.
* Refer to 3 for general export settings.

### 2.3 Output 2: Graph
* Short description.
* (Convex partitioning overlayed with the) nodes and edges of the graph.
* Refer to 3 for general export settings.

### 2.4 Labelling the graph
* Baking the graph geometry.
* Assigning the 3D points (correspond to nodes) to the different semantic labels.

### 2.4 Output 3: ICD
* Short description (reference to paper).
* Here display graph with numbers indicating the degree.
* Render with ICD displayed on the side.

### 2.5 Output 4: YTI
* Short description (reference to paper).
* Check the YTI setting.
* Render with YTI displayed on the side.

### 2.6 Output 5: SCI
* Short description (reference to paper).
* Check the SCI setting.
* Render with SCI displayed on the side.

## 3. Your output
Here we explain how you need to render.

### 3.1 Viewport definitions
* Align center of viewport with center of floorplan.
* Rendering mode arctic (-> customize settings with rendering)

### 3.2 Rendering and exporting
* ViewCaptureToFile
* Constrain resolution, dpi etc.
* Upload the images named correctly to polybox.
* Download this .md file and fill in the details.
-->
* * *

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
