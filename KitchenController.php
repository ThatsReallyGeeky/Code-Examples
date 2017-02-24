<?php

namespace Frontend\Controller;

use Shared\Entity\KitchenStyle;
use Symfony\Bundle\FrameworkBundle\Controller\Controller;
use Symfony\Component\HttpFoundation\Response;

class KitchenController extends Controller
{

    public function indexAction($slug)
    {

        $isKitchenView = $this->get('shared.kitchen')->isKitchenView($slug);

        if($isKitchenView){

            return $this->forward('Frontend:Kitchen:singleView', array(
                'slug' => $slug,
            ));

        }

        return $this->forward('Frontend:Kitchen:gridView', array(
            'slug' => $slug,
        ));

    }

    public function gridViewAction($slug)
    {

        $slug = ($slug == null) ? 'default':$slug;

        $currentStyle = $this->getDoctrine()->getRepository('Shared:KitchenStyle')->findOneBy(array(
            'slug' => $slug
        ));

        if(!$currentStyle){

            return $this->redirectToRoute('kitchens');

        }

        return $this->render('Frontend:Kitchen:grid.html.twig', array(
            'currentStyle' => $currentStyle,
            'filterUrl' => $this->generateUrl('ajax_filter_kitchen_grid', array(
                'slug' => $slug,
            )),
        ));

    }

    public function singleViewAction($slug)
    {

        $kitchen = $this->getDoctrine()->getRepository('Shared:Kitchen')->findOneBy(array(
            'slug' => $slug,
        ));

        $kitchenFinishes = $this->getDoctrine()->getRepository('Shared:KitchenFinishDetail')->findBy(array(
            'kitchen' => $kitchen->getId(),
        ));

        return $this->render('Frontend:Kitchen:single.html.twig', array(
            'kitchen' => $kitchen,
            'kitchenFinishes' => $kitchenFinishes,
            'slug' => $slug,
        ));

    }

    public function externalReviewsPartialAction()
    {

        return $this->render('Frontend:Kitchen/Partial:external-reviews.html.twig');

    }

    public function reviewsPartialAction(KitchenStyle $style = null)
    {

        $style = ($style->isHidden()) ? null:$style;

        return $this->render('Frontend:Kitchen/Partial:reviews.html.twig', array(
            'style' => $style,
        ));

    }

    public function helpPartialAction()
    {

        return $this->render('Frontend:Kitchen/Partial:help.html.twig');

    }

}