"use client";

import { useParams } from "next/navigation";
import { AdvertisingDetailPanel } from "@/components/advertising-workspace";

export default function AdDetailPage() {
  const params = useParams<{ adId: string }>();
  return <AdvertisingDetailPanel kind="ad" id={params.adId} />;
}
