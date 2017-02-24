<?php

namespace Shared\Repository;

use Doctrine\ORM\EntityRepository;
use Shared\Filters\KitchenFilter;

class KitchenRepository extends EntityRepository
{

    public function getPopularKitchensArray()
    {

        $query = $this->createQueryBuilder('k');
        $query->select('k.id', 'k.name', 'k.slug');
        $query->where('k.active = 1');
        $query->setMaxResults(5);

        $result = $query->getQuery();

        return $result->getArrayResult();

    }

    public function getKitchensByFilterArray(KitchenFilter $kitchenFilter)
    {

        $options = $kitchenFilter->getOptions();

        $query = $this->createQueryBuilder('k');

        if($options['slug'] && $options['slug'] != 'default'){

            $query->join('k.styles', 's');
            $query->andWhere('s.slug = :slug');
            $query->setParameter(':slug', $options['slug']);

        }

        if($options['finishType']){

            $query->join('k.finishTypes', 'ft');
            $query->andWhere('ft.id = :finishType');
            $query->setParameter(':finishType', $options['finishType']);

        }

        if($options['material']){

            $query->join('k.materials', 'm');
            $query->andWhere('m.id = :material');
            $query->setParameter(':material', $options['material']);

        }

        if($options['finish']){

            $query->join('k.finishes', 'f');
            $query->andWhere('f.id = :finish');
            $query->setParameter(':finish', $options['finish']);

        }

        if($options['active']){

            $query->andWhere('k.active = 1');

        }

        $result = $query->getQuery();

        return $result->getArrayResult();

    }

}